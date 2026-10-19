using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Services;

namespace MockModule.Service.Controllers
{
    [ApiController]
    [Route("files")]
    public class FileDeleteController : ControllerBase
    {
        private readonly FilesService _filesService;
        private readonly ScopeReaderService _scopeReader;

        public FileDeleteController(FilesService filesService, ScopeReaderService scopeReader)
        {
            _filesService = filesService;
            _scopeReader = scopeReader;
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteFile(string fileId)
        {
            var scope = await _scopeReader.ReadAsync(Request);

            if (scope is null)
            {
                return Unauthorized();
            }

            if (!scope.Has("files.write"))
            {
                return StatusCode(
                    403,
                    new { StatusCode = "MISSING_PERMISSIONS" });
            }

            if (string.IsNullOrWhiteSpace(fileId))
            {
                return BadRequest(new { code = "MISSING_FILE_ID" });
            }

            var result = await _filesService.DeleteFileAsync(
                fileId,
                scope.TenantId,
                scope.MemberID);

            return Ok(result);
        }



    }
}
