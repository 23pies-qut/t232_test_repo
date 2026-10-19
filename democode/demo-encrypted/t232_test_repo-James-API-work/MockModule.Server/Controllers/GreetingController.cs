using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Services;

namespace MockModule.Server.Controllers
{
    [Route("api/[controller]")] // controller = name of file minus 'Controller' at the end. 
    [ApiController]
    public class GreetingController : ControllerBase
    {
        // POST /greet
        [HttpPost("greet")]
        public IActionResult Greet([FromBody] Greeting request)
        {
            return Ok(new { message = $"Hello, {request.Name}!" });
        }
    }
}
