// https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0&tabs=visual-studio

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Services;

namespace MockModule.Server.Controllers
{
    public record SetUserRequest(string UserName);

    [Route("api/[controller]")]
    [ApiController]
    public class UserContextController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public UserContextController(IWebHostEnvironment envorionment, IConfiguration configuration, TokenService tokenService)
        {
            _environment = envorionment;
            _configuration = configuration;
            _tokenService = tokenService;
        }

        // FOR TESTING - Users are preadded in the db as test subjects with test files.
        private static readonly Dictionary<string, (string MemberId, string TenantId)> TestUsers = new()
        {
            ["Andy"] = ("00000000000000000000000000000001", "00000000000000000000000000000001"),
            ["Bob"] = ("00000000000000000000000000000002", "00000000000000000000000000000002")
        };

        [HttpPost("set-user")]
        public IActionResult SetUser([FromBody] SetUserRequest request)
        {
            var standalone = _environment.IsDevelopment() &&
                string.Equals(_configuration["MockModuleStandalone:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
           
            Console.WriteLine("Standalone enabled: " +
                _configuration["MockModuleStandalone:Enabled"]);
            Console.WriteLine("Is Development: " + _environment.IsDevelopment());

            if (!standalone) return NotFound();

            if (!TestUsers.TryGetValue(request.UserName, out var user))
                return BadRequest(new { code = "MOCKMODULE_USER_UNKNOWN" });

            var permissions = new[] { "files.read", "files.write" };
            var token = _tokenService.CreateToken(user.TenantId, user.MemberId, permissions, duration: 2);

            return Ok(new { token });
        }
    }
}
