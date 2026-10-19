using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Models;
using MockModule.Server.Services;


namespace MockModule.Server.Controllers
{



 
    [Route("api/[controller]")]
    [ApiController]
    public class FileDownloadController : ControllerBase
    {
        private readonly FilesService _filesService;
        private readonly ScopeReaderService _scopeReader;
        public  FileDownloadController(FilesService filesService, ScopeReaderService scopeReader)
        {
            _filesService = filesService;
            _scopeReader = scopeReader;
        }

        [HttpGet("{fileId}/download")]
        public async Task<IActionResult> GetFileContentAsync(string fileId)
        {
            var scope = await _scopeReader.ReadAsync(Request);

            if (scope is null)
            {
                return Unauthorized();
            }

            if (!scope.Has("files.read"))
            {
                return StatusCode(
                    403,
                    new { StatusCode = "MISSING_PERMISSIONS" });
            }

            if (string.IsNullOrWhiteSpace(fileId))
            {
                return BadRequest(new { code = "MISSING_FILE_ID" });
            }

            //Retrieve files for the validated tenant and member
            var file = await _filesService.GetFileContentAsync(
                fileId,
                scope.TenantId,
                scope.MemberID);
            if (file is null)
            {
                return NotFound(new { code = "FILE_NOT_FOUND" });
            }

            // TODO: Decrypt the file content before returning the download.
            // Return the stored file content as a download.
            // Encryption/decryption is intentionally out of scope for now.
            return File(file.Ciphertext, "image/jpeg", file.FileName);
        }
    }
}