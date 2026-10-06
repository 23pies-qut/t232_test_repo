using System.Data;
using Microsoft.Data.SqlClient;

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
            await SetSessionContextAsync(db, tenantId, memberId);

            await using var cmd = new SqlCommand("dbo.files_CRUD_JSON", db) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Action", "SELECT");
            cmd.Parameters.AddWithValue("@Payload", "{}");
            return (await cmd.ExecuteScalarAsync())?.ToString() ?? "[]";
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