using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Services;

namespace MockModule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadFilesController : ControllerBase
    {
        // Fields
        private readonly ScopeReaderService _scopeReader;

        public UploadFilesController(ScopeReaderService scopeReader)
        {
            _scopeReader = scopeReader;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var scope = await _scopeReader.ReadAsync(Request);
            if(scope is null)
            {
                return Unauthorized();
            }
            Console.WriteLine("Permissions: " + string.Join(", ", scope.Permissions));
            if (!scope.Has("files.write"))
            {
                return StatusCode(403, new { StatusCode = "MISSING_PERMISSIONS" });
            }
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { code = "MISSING_FILE" });
            }

            return Ok(new { recievedFileName = file.FileName, recievedBytes = file.Length });
        }
    }
}
