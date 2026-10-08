using System.Data;
using System.Text.Json;
using IdentityKeyApi.Models;
using Microsoft.Data.SqlClient;

namespace IdentityKeyApi.Services;

public class FileService : IFileService
{
    private const string CrudProcedure = "dbo.files_CRUD_JSON";

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly string _connectionString;
    private readonly ITenantContextService _tenantContext;
    private readonly IEncryptionService _encryptionService;

    public FileService(
        IConfiguration configuration,
        ITenantContextService tenantContext,
        IEncryptionService encryptionService)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection was not found."
            );

        _tenantContext = tenantContext;
        _encryptionService = encryptionService;
    }

    public async Task<List<FileMetadata>> GetFilesAsync()
    {
        await using SqlConnection connection =
            await OpenConnectionAsync();

        object? result =
            await ExecuteCrudAsync(connection, "SELECT", "{}");

        string json = result?.ToString() ?? "[]";

        return JsonSerializer.Deserialize<
                   List<FileMetadata>
               >(json, JsonOptions)
               ?? [];
    }

    public async Task<FileUploadResult> UploadFileAsync(
        string fileName,
        string mimeType,
        byte[] content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "File name is required.",
                nameof(fileName)
            );
        }

        if (content.Length == 0)
        {
            throw new ArgumentException(
                "The uploaded file is empty.",
                nameof(content)
            );
        }

        string safeFileName =
            Path.GetFileName(fileName);

        string safeMimeType =
            string.IsNullOrWhiteSpace(mimeType)
                ? "application/octet-stream"
                : mimeType;

        EncryptedFileData encryptedData =
            _encryptionService.Encrypt(content);

        await using SqlConnection connection =
            await OpenConnectionAsync();

        string fileId =
            await InsertFileMetadataAsync(
                connection,
                safeFileName,
                safeMimeType,
                content.LongLength
            );

        await StoreEncryptedContentAsync(
            connection,
            fileId,
            encryptedData
        );

        return new FileUploadResult
        {
            FileId = fileId,
            FileName = safeFileName,
            Status = "UPLOADED"
        };
    }

    public async Task<FileDownloadData?> DownloadFileAsync(
        string fileId)
    {
        string normalisedFileId =
            NormaliseFileId(fileId);

        await using SqlConnection connection =
            await OpenConnectionAsync();

        await using SqlCommand command =
            new("dbo.files_Content_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.Add(
            "@FileID",
            SqlDbType.Char,
            36
        ).Value = normalisedFileId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                CommandBehavior.SingleRow
            );

        if (!await reader.ReadAsync())
        {
            return null;
        }

        EncryptedFileData encryptedData =
            new()
            {
                Ciphertext =
                    reader.GetFieldValue<byte[]>(
                        reader.GetOrdinal("ciphertext")
                    ),

                Nonce =
                    reader.GetFieldValue<byte[]>(
                        reader.GetOrdinal("nonce")
                    ),

                AuthTag =
                    reader.GetFieldValue<byte[]>(
                        reader.GetOrdinal("auth_tag")
                    ),

                WrappedDek =
                    reader.GetFieldValue<byte[]>(
                        reader.GetOrdinal("wrapped_dek")
                    ),

                PlaintextChecksum =
                    reader.GetString(
                        reader.GetOrdinal("plaintext_checksum")
                    )
            };

        byte[] plaintext =
            _encryptionService.Decrypt(encryptedData);

        return new FileDownloadData
        {
            FileName =
                reader.GetString(
                    reader.GetOrdinal("filename")
                ),

            MimeType =
                reader.GetString(
                    reader.GetOrdinal("mime_type")
                ),

            Content = plaintext
        };
    }

    public async Task<bool> DeleteFileAsync(
        string fileId)
    {
        string normalisedFileId =
            NormaliseFileId(fileId);

        await using SqlConnection connection =
            await OpenConnectionAsync();

        string payload =
            JsonSerializer.Serialize(
                new { fileId = normalisedFileId }
            );

        object? result =
            await ExecuteCrudAsync(
                connection,
                "DELETE",
                payload
            );

        return result is not null &&
               result != DBNull.Value;
    }

    private async Task<string> InsertFileMetadataAsync(
        SqlConnection connection,
        string fileName,
        string mimeType,
        long sizeBytes)
    {
        string payload =
            JsonSerializer.Serialize(
                new
                {
                    filename = fileName,
                    mimeType,
                    sizeBytes
                }
            );

        object? result =
            await ExecuteCrudAsync(
                connection,
                "INSERT",
                payload
            );

        string resultJson =
            result?.ToString()
            ?? throw new InvalidOperationException(
                "The database did not return a file ID."
            );

        FileIdResult? fileIdResult =
            JsonSerializer.Deserialize<FileIdResult>(
                resultJson,
                JsonOptions
            );

        if (string.IsNullOrWhiteSpace(
                fileIdResult?.FileId))
        {
            throw new InvalidOperationException(
                "The returned file ID is invalid."
            );
        }

        return fileIdResult.FileId.Trim();
    }

    private static async Task StoreEncryptedContentAsync(
        SqlConnection connection,
        string fileId,
        EncryptedFileData encryptedData)
    {
        await using SqlCommand command =
            new("dbo.files_Content_Put", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.Add(
            "@FileID",
            SqlDbType.Char,
            36
        ).Value = fileId;

        command.Parameters.Add(
            "@Ciphertext",
            SqlDbType.VarBinary,
            -1
        ).Value = encryptedData.Ciphertext;

        command.Parameters.Add(
            "@Nonce",
            SqlDbType.VarBinary,
            12
        ).Value = encryptedData.Nonce;

        command.Parameters.Add(
            "@AuthTag",
            SqlDbType.VarBinary,
            16
        ).Value = encryptedData.AuthTag;

        command.Parameters.Add(
            "@WrappedDek",
            SqlDbType.VarBinary,
            512
        ).Value = encryptedData.WrappedDek;

        command.Parameters.Add(
            "@Checksum",
            SqlDbType.VarChar,
            128
        ).Value = encryptedData.PlaintextChecksum;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ExecuteCrudAsync(
        SqlConnection connection,
        string action,
        string payload)
    {
        await using SqlCommand command =
            new(CrudProcedure, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

        command.Parameters.Add(
            "@Action",
            SqlDbType.VarChar,
            12
        ).Value = action;

        command.Parameters.Add(
            "@Payload",
            SqlDbType.NVarChar,
            -1
        ).Value = payload;

        return await command.ExecuteScalarAsync();
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync();

        const string sql = """
            EXEC sys.sp_set_session_context
                @key = N'TenantID',
                @value = @TenantID;

            EXEC sys.sp_set_session_context
                @key = N'MemberID',
                @value = @MemberID;
            """;

        await using SqlCommand command =
            new(sql, connection);

        command.Parameters.Add(
            "@TenantID",
            SqlDbType.Char,
            36
        ).Value = _tenantContext.TenantId;

        command.Parameters.Add(
            "@MemberID",
            SqlDbType.Char,
            36
        ).Value = _tenantContext.MemberId;

        await command.ExecuteNonQueryAsync();

        return connection;
    }

    private static string NormaliseFileId(
        string fileId)
    {
        if (!Guid.TryParse(
                fileId,
                out Guid parsedFileId))
        {
            throw new ArgumentException(
                "The file ID is invalid.",
                nameof(fileId)
            );
        }

        return parsedFileId.ToString("N");
    }

    private sealed class FileIdResult
    {
        public string FileId { get; set; } =
            string.Empty;
    }
}
