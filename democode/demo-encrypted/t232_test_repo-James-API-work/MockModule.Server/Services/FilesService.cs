using System.Data;
using Microsoft.Data.SqlClient;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    public class FilesService
    {
        private readonly string _connectionString;
        public FilesService(IConfiguration configuration) =>
            _connectionString = configuration.GetConnectionString("MockModuleData")!;

        public async Task<string> ListFilesAsync(string tenantId, string memberId)
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();
            
            Console.WriteLine($"Connected SQL Server: {db.DataSource}");
            Console.WriteLine($"Connected database: {db.Database}");

            await SetSessionContextAsync(db, tenantId, memberId);

            await using var cmd = new SqlCommand("dbo.files_CRUD_JSON", db) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Action", "SELECT");
            cmd.Parameters.AddWithValue("@Payload", "{}");
            return (await cmd.ExecuteScalarAsync())?.ToString() ?? "[]";
        }

        public async Task<string> CreateFileAsync(
            string fileName,
            string mimeType,
            long sizeBytes,
            string tenantId,
            string memberId)
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();

            await SetSessionContextAsync(db, tenantId, memberId);

            await using var cmd = new SqlCommand(
                "dbo.files_CRUD_JSON",
                db)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "INSERT");

            cmd.Parameters.AddWithValue(
                "@Payload",
                $@"{{
                    ""filename"": ""{fileName}"",
                    ""mimeType"": ""{mimeType}"",
                    ""sizeBytes"": {sizeBytes}
        }}");

            return (await cmd.ExecuteScalarAsync())?.ToString() ?? "{}";
        }

        public async Task SaveFileContentAsync(
            string fileId,
            EncryptedFileData encrypted,
            string tenantId,
            string memberId)
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();

            await SetSessionContextAsync(db, tenantId, memberId);

            await using var cmd = new SqlCommand("dbo.files_Content_Put", db)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@FileID", SqlDbType.Char, 32).Value = fileId;
            cmd.Parameters.Add("@Ciphertext", SqlDbType.VarBinary, -1).Value = encrypted.Ciphertext;
            cmd.Parameters.Add("@Nonce", SqlDbType.VarBinary, 16).Value = encrypted.Nonce;
            cmd.Parameters.Add("@AuthTag", SqlDbType.VarBinary, 16).Value = encrypted.AuthTag;
            cmd.Parameters.Add("@WrappedDek", SqlDbType.VarBinary, 512).Value = encrypted.WrappedDek;
            cmd.Parameters.Add("@Checksum", SqlDbType.VarChar, 128).Value = encrypted.PlaintextChecksum;

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<FileDownload?> GetFileContentAsync(
             string fileId,
             string tenantId,
             string memberId)
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();

            await SetSessionContextAsync(db, tenantId, memberId);

            await using var cmd = new SqlCommand(
                "dbo.files_Content_Get", db)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@FileID", SqlDbType.Char, 32).Value = fileId;

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new FileDownload
            {
                Ciphertext = (byte[])reader["ciphertext"],
                Nonce = (byte[])reader["nonce"],
                AuthTag = (byte[])reader["auth_tag"],
                WrappedDek = (byte[])reader["wrapped_dek"],
                PlaintextChecksum =
                    reader["plaintext_checksum"].ToString()!,
                FileName = reader["filename"].ToString()!
            };
        }

        public async Task<string> DeleteFileAsync(
            string fileId,
            string tenantId,
            string memberId)
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync();
            //sets the session context for the current connection to include the tenantId and memberId
            //So Session Context can use tenantId and memberId to determine who the request belongs to and what permissions they have
            await SetSessionContextAsync(db, tenantId, memberId);

            //Create SQL command to call the stored procedure "dbo.files_CRUD_JSON"
            //with the action "DELETE" and the payload containing the fileId
            await using var cmd = new SqlCommand("dbo.files_CRUD_JSON", db)
            {
                CommandType = CommandType.StoredProcedure
            };
            //Specifying we want the "DELETE" action from the stored procedure
            cmd.Parameters.AddWithValue("@Action", "DELETE");
            cmd.Parameters.AddWithValue(
                "@Payload",
                $"{{\"fileId\": \"{fileId}\"}}");
            //Execute the command and return the result as a string, or "{}" if the result is null
            return (await cmd.ExecuteScalarAsync())?.ToString() ?? "{}";
        }

        private static async Task SetSessionContextAsync(SqlConnection db, string tenantId, string memberId)
        {
            await using var tenantCmd = new SqlCommand("EXEC sys.sp_set_session_context @key, @value, @read_only = 1", db);
            tenantCmd.Parameters.AddWithValue("@key", "TenantID");
            tenantCmd.Parameters.AddWithValue("@value", tenantId);
            await tenantCmd.ExecuteNonQueryAsync();

            await using var memberCmd = new SqlCommand("EXEC sys.sp_set_session_context @key, @value, @read_only = 1", db);
            memberCmd.Parameters.AddWithValue("@key", "MemberID");
            memberCmd.Parameters.AddWithValue("@value", memberId);
            await memberCmd.ExecuteNonQueryAsync();
        }
        

    }
}