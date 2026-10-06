using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Models;
using MockModule.Server.Services;

namespace MockModule.Server.Controllers
{
    /// <summary>
    /// Selects a test user and tenant and returns a JWT (standalone mode).
    /// Available only in Development with MockModuleStandalone:Enabled = true; otherwise returns 404.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserContextController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly UserContextService _userContextService;

        public UserContextController(IWebHostEnvironment environment, IConfiguration configuration, UserContextService userContextService)
        {
            _environment = environment;
            _configuration = configuration;
            _userContextService = userContextService;
        }

        private bool Standalone => _environment.IsDevelopment() &&
            string.Equals(_configuration["MockModuleStandalone:Enabled"], "true", StringComparison.OrdinalIgnoreCase);

        // GET api/UserContext/users - test users and the tenants they belong to
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            if (!Standalone) return NotFound();
            return Ok(await _userContextService.GetUsersAsync());
        }

        // POST api/UserContext/set-user - issues a JWT for a user + tenant; also used to switch tenant
        [HttpPost("set-user")]
        public async Task<IActionResult> SetUser([FromBody] SetUserRequest request)
        {
            if (!Standalone) return NotFound();
            return Ok(await _userContextService.SetUserAsync(request));
        }
    }
}
