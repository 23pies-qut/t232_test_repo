using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MockModule.Server.Models;

namespace MockModule.Server.Services
{
    public class ScopeReaderService
    {
        // Fields:
        private readonly JsonWebTokenHandler _tokenHandler = new();
        private readonly TokenValidationParameters _parameters;

        // Constructor: 
        public ScopeReaderService(IConfiguration configuration)
        {
            var signingKey = configuration["MockModuleSecurity:SigningKeyBase64"]
                ?? throw new InvalidOperationException("No signing key.");

            _parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["MockModuleSecurity:Issuer"] ?? "mockmodule-test",
                ValidateAudience = true,
                ValidAudience = configuration["MockModuleSecurity:Audience"] ?? "mockmodule-filewidget",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        }
        public async Task<FileScope?> ReadAsync(HttpRequest request)
        {
            try
            {
                var header = request.Headers.Authorization.ToString();
                if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;

                var result = await _tokenHandler.ValidateTokenAsync(header[7..], _parameters);
                if (!result.IsValid || result.ClaimsIdentity is null) return null;

                string? Claim(string name) =>
                    result.ClaimsIdentity.Claims.FirstOrDefault(c => c.Type == name)?.Value;

                var tenantId = Claim("tenant_id");
                var memberId = Claim("member_id");
                if (tenantId?.Length != 32 || memberId?.Length != 32) return null;

                var permissions = result.ClaimsIdentity.Claims
                    .Where(c => c.Type == "permissions")
                    .Select(c => c.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return new FileScope(tenantId, memberId, permissions);
            }
            catch { return null; }
        }
    }
}
