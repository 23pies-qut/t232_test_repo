using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.SqlClient;
using MockModule.Server.Middleware;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    /// <summary>
    /// File business logic: reads and writes the files and file_contents tables through stored procedures.
    ///   List:     files_CRUD_JSON 'SELECT'
    ///   Upload:   sp_set_session_context -> files_CRUD_JSON 'INSERT' (PENDING) -> encrypt -> files_Content_Put (UPLOADED)
    ///   Download: files_Content_Get -> decrypt and verify
    ///   Delete:   files_CRUD_JSON 'DELETE' (soft delete)
    /// Every upload step is recorded in a StepTrace for the frontend's storage animation and stall detection.
    /// </summary>
    public class FilesService
    {
        public const long MaxBytes = 10 * 1024 * 1024; // 10 MB
        private const string CrudProcedure = "dbo.files_CRUD_JSON";
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        /// <summary>Order of the server-side upload steps (same as UPLOAD_STAGES in the frontend FileBrowserPage); used to name the failed step.</summary>
        private static readonly (string Node, string Title)[] UploadStages =
        [
            ("api", "Validate JWT and file"),
            ("proc", "Set session context"),
            ("proc", "files_CRUD_JSON  @Action = 'INSERT'"),
            ("api", "Envelope encryption (AES-256-GCM)"),
            ("proc", "files_Content_Put")
        ];

        private readonly SqlConnectionFactory _db;
        private readonly EncryptionService _encryption;
        private readonly UploadProgressService _progress;
        private readonly ILogger<FilesService> _logger;

        public FilesService(SqlConnectionFactory db, EncryptionService encryption, UploadProgressService progress, ILogger<FilesService> logger)
        {
            _db = db;
            _encryption = encryption;
            _progress = progress;
            _logger = logger;
        }

        /// <summary>All non-deleted files of the current tenant (RLS returns only this tenant's rows).</summary>
        public async Task<List<FileRow>> ListAsync(CurrentUser user)
        {
            await using var connection = await _db.OpenAsync(user);
            var files = await CallCrudAsync<List<FileRow>>(connection, "SELECT", new { }) ?? [];
            return files.Select(f => f with { FileId = SqlConnectionFactory.NormalizeId(f.FileId) }).ToList();
        }

        /// <summary>Steps an upload has completed so far (only the caller's own uploads).</summary>
        public IReadOnlyList<Step> GetUploadProgress(CurrentUser user, string uploadId) => _progress.Get(user.MemberId, uploadId);

        /// <summary>
        /// Uploads a file. When uploadId is set, progress is updated after every step so the frontend can query it.
        /// Any unexpected error (e.g. a SQL timeout) is recorded as a failed step and returned with the trace, so the frontend can show where it failed.
        /// </summary>
        public async Task<UploadResponse> UploadAsync(CurrentUser user, IFormFile? file, string? uploadId)
        {
            var tracked = !string.IsNullOrWhiteSpace(uploadId);
            Action<IReadOnlyList<Step>>? onChange = null;
            if (tracked) onChange = steps => _progress.Update(user.MemberId, uploadId!, steps);
            var trace = new StepTrace(onChange);
            try
            {
                return await UploadCoreAsync(user, file, trace);
            }
            catch (Exception ex) when (ex is not ApiException)
            {
                var (node, title) = UploadStages[Math.Min(trace.Steps.Count, UploadStages.Length - 1)];
                _logger.LogError(ex, "Upload failed at step {Step}", title);
                trace.Fail(node, title, $"{ex.GetType().Name}: {ex.Message}");
                throw new ApiException(500, "UPLOAD_FAILED", $"The upload failed at step \"{title}\".", trace.Steps);
            }
            finally
            {
                if (tracked) _progress.Remove(user.MemberId, uploadId!);
            }
        }

        /// <summary>Read ciphertext -> unwrap the data key -> decrypt -> verify SHA-256.</summary>
        public async Task<DownloadedFile> DownloadAsync(CurrentUser user, string rawId)
        {
            var fileId = ParseFileId(rawId);
            await using var connection = await _db.OpenAsync(user);

            string filename, checksum;
            EncryptedFile encrypted;
            await using (var command = Procedure(connection, "dbo.files_Content_Get"))
            {
                command.Parameters.Add(new SqlParameter("@FileID", SqlDbType.Char, 36) { Value = fileId });
                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new ApiException(404, "FILES_NOT_FOUND", "File not found, not fully uploaded, or owned by another tenant.");
                filename = reader.GetString(reader.GetOrdinal("filename"));
                checksum = reader.GetString(reader.GetOrdinal("plaintext_checksum"));
                encrypted = new EncryptedFile((byte[])reader["ciphertext"], (byte[])reader["nonce"], (byte[])reader["auth_tag"], (byte[])reader["wrapped_dek"]);
            }

            byte[] plaintext;
            try
            {
                plaintext = _encryption.Decrypt(encrypted, user.TenantId, fileId);
            }
            catch (CryptographicException ex)
            {
                // reached when the ciphertext, nonce or tag was changed or belongs to another tenant / file
                _logger.LogWarning(ex, "Decryption failed for file {FileId}", fileId);
                throw new ApiException(422, "INTEGRITY_FAILED", "The file could not be decrypted or failed its integrity check.");
            }
            if (!string.Equals(Sha256(plaintext), checksum, StringComparison.OrdinalIgnoreCase))
                throw new ApiException(422, "INTEGRITY_FAILED", "The file failed its integrity check.");

            var mimeType = ContentTypes.TryGetContentType(filename, out var type) ? type : "application/octet-stream";
            return new DownloadedFile(filename, mimeType, plaintext);
        }

        /// <summary>Soft delete (status = DELETED; the data is kept).</summary>
        public async Task DeleteAsync(CurrentUser user, string rawId)
        {
            var fileId = ParseFileId(rawId);
            await using var connection = await _db.OpenAsync(user);
            await CallCrudAsync<FileIdResult>(connection, "DELETE", new { fileId });
        }

        // ---- upload steps ---------------------------------------------------------------

        /// <summary>Validate -> write metadata (PENDING) -> envelope encryption -> write ciphertext (UPLOADED).</summary>
        private async Task<UploadResponse> UploadCoreAsync(CurrentUser user, IFormFile? file, StepTrace trace)
        {
            if (file is null || file.Length == 0)
                throw new ApiException(400, "FILE_EMPTY", "Choose a non-empty file (form field \"file\").");
            if (file.Length > MaxBytes)
                throw new ApiException(413, "FILE_TOO_LARGE", "Files are limited to 10 MB.");

            var filename = CleanFilename(file.FileName);
            var mimeType = string.IsNullOrWhiteSpace(file.ContentType) || file.ContentType.Length > 255 ? "application/octet-stream" : file.ContentType;
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);
            var plaintext = buffer.ToArray();
            var checksum = Sha256(plaintext);
            trace.Add("api", "Validate JWT and file", $"{user.Name} @ {user.TenantName} · {plaintext.Length:N0} B · SHA-256 {checksum[..12]}…");

            // 1. Set the session context (RLS filters by tenant using it)
            await using var connection = await _db.OpenAsync(user);
            trace.Add("proc", "Set session context", $"TenantID of {user.TenantName} written to this connection; RLS reads the tenant from here", "sys.sp_set_session_context",
                [
                    $"@key = N'TenantID', @value = '{StepTrace.Short(user.TenantId)}', @read_only = 1",
                    $"@key = N'MemberID', @value = '{StepTrace.Short(user.MemberId)}', @read_only = 1"
                ]);

            // 2. Metadata row, status PENDING
            var created = await CallCrudAsync<FileIdResult>(connection, "INSERT", new { filename, mimeType, sizeBytes = plaintext.LongLength });
            var fileId = SqlConnectionFactory.NormalizeId(created?.FileId);
            if (fileId.Length == 0)
                throw new InvalidOperationException("files_CRUD_JSON 'INSERT' returned no fileId.");
            trace.Add("proc", "files_CRUD_JSON  @Action = 'INSERT'", $"New file {StepTrace.Short(fileId)}, status PENDING", CrudProcedure,
                ["INSERT dbo.files (id, tenant_id, owner_id, filename, mime_type, size_bytes, status) … 'PENDING'"],
                [StepTrace.Row("dbo.files", "INSERT", ("id", StepTrace.Short(fileId)), ("filename", filename), ("status", "PENDING"))]);

            try
            {
                // 3. Envelope encryption
                var encrypted = _encryption.Encrypt(plaintext, user.TenantId, fileId);
                trace.Add("api", "Envelope encryption (AES-256-GCM)", $"Random data key encrypts the file; the master key wraps the data key ({encrypted.WrappedDek.Length} B)", cipher: true);

                // 4. Ciphertext row + status UPLOADED
                await PutContentAsync(connection, fileId, encrypted, checksum);
                trace.Add("proc", "files_Content_Put", "Ciphertext written to file_contents and status set to UPLOADED (one transaction)", "dbo.files_Content_Put",
                    ["INSERT dbo.file_contents (…)", "UPDATE dbo.files SET status = 'UPLOADED'"],
                    [
                        StepTrace.Row("dbo.file_contents", "INSERT", ("file_id", StepTrace.Short(fileId)), ("ciphertext", $"{StepTrace.Hex(encrypted.Ciphertext)} ({encrypted.Ciphertext.Length:N0} B)")),
                        StepTrace.Row("dbo.files", "UPDATE", ("id", StepTrace.Short(fileId)), ("status", "UPLOADED"))
                    ], cipher: true);
            }
            catch
            {
                // if storing the content fails, soft-delete the PENDING row so it does not stay in the list
                try { await CallCrudAsync<FileIdResult>(connection, "DELETE", new { fileId }); }
                catch (Exception ex) { _logger.LogError(ex, "Could not discard pending file {FileId}", fileId); }
                throw;
            }

            return new UploadResponse(fileId, filename, plaintext.LongLength, checksum, trace.Steps);
        }

        // ---- helpers ----------------------------------------------------------------------

        /// <summary>
        /// Calls files_CRUD_JSON: @Action selects the operation, @Payload carries JSON, the result is JSON.
        /// SQL Server splits a top-level FOR JSON result into rows of about 2 KB (NULL when empty), so the rows are concatenated.
        /// </summary>
        private static async Task<T?> CallCrudAsync<T>(SqlConnection connection, string action, object payload)
        {
            await using var command = Procedure(connection, CrudProcedure);
            command.Parameters.Add(new SqlParameter("@Action", SqlDbType.VarChar, 12) { Value = action });
            command.Parameters.Add(new SqlParameter("@Payload", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(payload, Json) });

            var json = new StringBuilder();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    if (!reader.IsDBNull(0)) json.Append(reader.GetString(0));
            }
            return json.Length == 0 ? default : JsonSerializer.Deserialize<T>(json.ToString(), Json);
        }

        /// <summary>Calls files_Content_Put in a transaction so its INSERT and UPDATE succeed or roll back together.</summary>
        private static async Task PutContentAsync(SqlConnection connection, string fileId, EncryptedFile e, string checksum)
        {
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            await using (var command = Procedure(connection, "dbo.files_Content_Put"))
            {
                command.Transaction = transaction;
                command.Parameters.Add(new SqlParameter("@FileID", SqlDbType.Char, 36) { Value = fileId });
                command.Parameters.Add(new SqlParameter("@Ciphertext", SqlDbType.VarBinary, -1) { Value = e.Ciphertext });
                command.Parameters.Add(new SqlParameter("@Nonce", SqlDbType.VarBinary, 16) { Value = e.Nonce });
                command.Parameters.Add(new SqlParameter("@AuthTag", SqlDbType.VarBinary, 16) { Value = e.AuthTag });
                command.Parameters.Add(new SqlParameter("@WrappedDek", SqlDbType.VarBinary, 512) { Value = e.WrappedDek });
                command.Parameters.Add(new SqlParameter("@Checksum", SqlDbType.VarChar, 128) { Value = checksum });
                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        private static SqlCommand Procedure(SqlConnection connection, string name) =>
            new(name, connection) { CommandType = CommandType.StoredProcedure };

        private static string ParseFileId(string? raw)
        {
            var id = SqlConnectionFactory.NormalizeId(raw).Replace("-", "");
            return id.Length == 32 && id.All(Uri.IsHexDigit) ? id : throw new ApiException(400, "FILE_ID_INVALID", "Invalid file id.");
        }

        private static string CleanFilename(string? raw)
        {
            var name = Path.GetFileName(raw ?? "").Trim();
            if (name.Length == 0) name = "unnamed";
            return name.Length > 512 ? name[^512..] : name; // files.filename is VARCHAR(512)
        }

        private static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }
}
