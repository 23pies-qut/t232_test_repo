using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MockModule.Server.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IConfiguration configuration) {
            _configuration = configuration;
            }

        public string CreateToken(string tenantId, string memberId, IEnumerable<string> permissions, int duration)
        {
            var key = _configuration["MockModuleSecurity:SigningKeyBase64"]
                ?? throw new InvalidOperationException("No signing key.");

            var tokenClaims = new List<Claim>
            {
                new Claim("tenant_id", tenantId),
                new Claim("member_id", memberId)
            };
            tokenClaims.AddRange(permissions.Select(p => new Claim("permissions", p)));

            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer:     _configuration["MockModuleSecurity:Issuer"]     ?? "mockmodule-test",
                audience:   _configuration["MockModuleSecurity:Audience"]   ?? "mockmodule-filewidget",
                claims:     tokenClaims,
                notBefore:  now,
                expires:    now.AddHours(duration),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Convert.FromBase64String(key)),
                    SecurityAlgorithms.HmacSha256
                )
            );

            return new JwtSecurityTokenHandler().WriteToken( token );
        }

    }
}
