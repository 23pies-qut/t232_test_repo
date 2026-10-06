using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    /// <summary>
    /// Issues and validates JWTs: claims tenant_id / member_id / permissions,
    /// settings MockModuleSecurity:SigningKeyBase64 / Issuer / Audience, HMAC-SHA256 signature.
    /// </summary>
    public class TokenService
    {
        public const string SigningKeySetting = "MockModuleSecurity:SigningKeyBase64";

        // Permission names; Program.cs registers an authorization policy with the same name for each
        public const string FilesRead = "files.read";
        public const string FilesUpload = "files.upload";
        public const string FilesDelete = "files.delete";
        public static readonly string[] Permissions = [FilesRead, FilesUpload, FilesDelete];

        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string Token, DateTime ExpiresAtUtc) CreateToken(TestUser user, Tenant tenant)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddHours(_configuration.GetValue<int?>("MockModuleSecurity:TokenHours") ?? 2);

            var claims = new List<Claim>
            {
                new(CurrentUser.ClaimTenantId, tenant.TenantId),
                new(CurrentUser.ClaimMemberId, user.UserId),
                new(CurrentUser.ClaimTenantName, tenant.TenantName),
                new(CurrentUser.ClaimName, user.Name)
            };
            claims.AddRange(Permissions.Select(p => new Claim(CurrentUser.ClaimPermissions, p)));

            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer(_configuration),
                Audience = Audience(_configuration),
                IssuedAt = now,
                NotBefore = now,
                Expires = expires,
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = new SigningCredentials(SigningKey(_configuration), SecurityAlgorithms.HmacSha256)
            });
            return (token, expires);
        }

        /// <summary>Validation rules for JwtBearer: issuer, audience, signature and lifetime are all checked.</summary>
        public static TokenValidationParameters ValidationParameters(IConfiguration configuration) => new()
        {
            ValidIssuer = Issuer(configuration),
            ValidAudience = Audience(configuration),
            IssuerSigningKey = SigningKey(configuration),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        private static string Issuer(IConfiguration c) => c["MockModuleSecurity:Issuer"] ?? "mockmodule-test";
        private static string Audience(IConfiguration c) => c["MockModuleSecurity:Audience"] ?? "mockmodule-filewidget";
        private static SymmetricSecurityKey SigningKey(IConfiguration c) =>
            new(Convert.FromBase64String(c[SigningKeySetting] ?? throw new InvalidOperationException($"{SigningKeySetting} is missing.")));
    }
}
