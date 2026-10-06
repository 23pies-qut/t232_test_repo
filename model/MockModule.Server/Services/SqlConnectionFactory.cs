using System.Data;
using Microsoft.Data.SqlClient;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    /// <summary>
    /// The only place that opens SQL Server connections. With a CurrentUser it writes the tenant and member into SESSION_CONTEXT (read-only);
    /// the stored procedures and the RLS policy read the current tenant from there.
    /// </summary>
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            // the connection string is in appsettings.Development.json
            _connectionString = configuration.GetConnectionString("MockModuleData")
                ?? throw new InvalidOperationException("Connection string ConnectionStrings:MockModuleData is missing (see appsettings.Development.json).");
        }

        /// <summary>Ids are 32 characters stored in CHAR(36) and come back space-padded: trim and upper-case them.</summary>
        public static string NormalizeId(string? id) => (id ?? "").Trim().ToUpperInvariant();

        public async Task<SqlConnection> OpenAsync(CurrentUser? user = null)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                if (user is not null)
                {
                    await using var command = new SqlCommand(
                        "EXEC sys.sp_set_session_context @key = N'TenantID', @value = @TenantID, @read_only = 1;" +
                        "EXEC sys.sp_set_session_context @key = N'MemberID', @value = @MemberID, @read_only = 1;", connection);
                    command.Parameters.Add(new SqlParameter("@TenantID", SqlDbType.Char, 36) { Value = user.TenantId });
                    command.Parameters.Add(new SqlParameter("@MemberID", SqlDbType.Char, 36) { Value = user.MemberId });
                    await command.ExecuteNonQueryAsync();
                }
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        /// <summary>Whether the database is reachable and the tables and stored procedures are installed (used by HealthController).</summary>
        public async Task<HealthReport> CheckAsync()
        {
            try
            {
                await using var connection = await OpenAsync();
                await using var command = new SqlCommand(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.files_CRUD_JSON') IS NULL OR OBJECT_ID(N'dbo.tenant_memberships') IS NULL THEN 0 ELSE 1 END;", connection);
                return (int)(await command.ExecuteScalarAsync())! == 1
                    ? new HealthReport(true, "Database is ready.")
                    : new HealthReport(false, "Connected, but the tables and stored procedures were not found: run MockModuleDB\\setup-database.ps1.");
            }
            catch (Exception ex)
            {
                return new HealthReport(false, $"Cannot connect to the database ({ex.Message}). Check the MockModuleData connection string in appsettings.Development.json.");
            }
        }
    }
}
