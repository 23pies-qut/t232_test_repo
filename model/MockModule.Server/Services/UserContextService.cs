using Microsoft.Data.SqlClient;
using MockModule.Server.Middleware;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    /// <summary>
    /// Test users and tenant switching (standalone mode).
    /// Reads the users / tenants / tenant_memberships tables and issues a token after checking membership.
    /// Switching tenant = issuing a new JWT for the same user with a different tenant_id.
    /// </summary>
    public class UserContextService
    {
        private readonly SqlConnectionFactory _db;
        private readonly TokenService _tokenService;

        public UserContextService(SqlConnectionFactory db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        /// <summary>All test users with their tenants (one row per user-tenant pair, grouped by user).</summary>
        public async Task<List<TestUser>> GetUsersAsync()
        {
            await using var connection = await _db.OpenAsync();
            await using var command = new SqlCommand("""
                SELECT u.id, u.name, u.email, t.id, t.name
                FROM dbo.users u
                LEFT JOIN dbo.tenant_memberships m ON m.user_id = u.id
                LEFT JOIN dbo.tenants t ON t.id = m.tenant_id
                ORDER BY u.name, t.name;
                """, connection);

            var users = new List<TestUser>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = SqlConnectionFactory.NormalizeId(reader.GetString(0));
                var user = users.Find(u => u.UserId == id);
                if (user is null)
                {
                    user = new TestUser(id, reader.GetString(1), reader.GetString(2), []);
                    users.Add(user);
                }
                if (!reader.IsDBNull(3))
                    user.Tenants.Add(new Tenant(SqlConnectionFactory.NormalizeId(reader.GetString(3)), reader.GetString(4)));
            }
            return users;
        }

        /// <summary>Checks membership and issues a JWT for the user + tenant.</summary>
        public async Task<SessionResponse> SetUserAsync(SetUserRequest request)
        {
            var users = await GetUsersAsync();
            var user = string.IsNullOrWhiteSpace(request.UserId)
                ? users.Find(u => string.Equals(u.Name, request.UserName?.Trim(), StringComparison.OrdinalIgnoreCase))
                : users.Find(u => u.UserId == SqlConnectionFactory.NormalizeId(request.UserId));
            if (user is null)
                throw new ApiException(400, "MOCKMODULE_USER_UNKNOWN", "Unknown test user.");

            var tenant = string.IsNullOrWhiteSpace(request.TenantId)
                ? user.Tenants.FirstOrDefault()
                : user.Tenants.Find(t => t.TenantId == SqlConnectionFactory.NormalizeId(request.TenantId));
            if (tenant is null)
                throw new ApiException(403, "NOT_A_MEMBER", $"{user.Name} is not a member of that tenant.");

            var (token, expires) = _tokenService.CreateToken(user, tenant);
            return new SessionResponse(token, expires, user.UserId, user.Name, tenant.TenantId, tenant.TenantName, user.Tenants);
        }
    }
}
