using System.Security.Claims;

namespace MockModule.Server.Models
{
    /// <summary>A tenant (one row of the tenants table).</summary>
    public record Tenant(string TenantId, string TenantName);

    /// <summary>A test user (users table) and the tenants they belong to (tenant_memberships table).</summary>
    public record TestUser(string UserId, string Name, string Email, List<Tenant> Tenants);

    /// <summary>Request body of POST api/UserContext/set-user: select the user by userId or userName; tenantId is optional.</summary>
    public record SetUserRequest(string? UserId, string? UserName, string? TenantId);

    /// <summary>Response of set-user: the JWT and the information shown in the frontend header.</summary>
    public record SessionResponse(string Token, DateTime ExpiresAtUtc, string UserId, string Name,
        string TenantId, string TenantName, IReadOnlyList<Tenant> Tenants);

    /// <summary>The user and tenant the current request acts as, read from the validated JWT claims.</summary>
    public record CurrentUser(string MemberId, string TenantId, string TenantName, string Name)
    {
        // JWT claim names
        public const string ClaimTenantId = "tenant_id";
        public const string ClaimMemberId = "member_id";
        public const string ClaimTenantName = "tenant_name";
        public const string ClaimName = "name";
        public const string ClaimPermissions = "permissions";

        public static CurrentUser From(ClaimsPrincipal principal) => new(
            Read(principal, ClaimMemberId), Read(principal, ClaimTenantId), Read(principal, ClaimTenantName), Read(principal, ClaimName));

        private static string Read(ClaimsPrincipal principal, string type) =>
            principal.FindFirst(type)?.Value ?? throw new InvalidOperationException($"The token is missing the {type} claim.");
    }
}
