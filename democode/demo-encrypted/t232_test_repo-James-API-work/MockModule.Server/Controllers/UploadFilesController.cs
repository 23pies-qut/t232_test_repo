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
        private readonly FilesService _filesService;
        private readonly ScopeReaderService _scopeReader;
        private readonly IEncryptionService _encryption;

        public UploadFilesController(
            ScopeReaderService scopeReader,
            FilesService fileService,
            IEncryptionService encryption)
        {
            _scopeReader = scopeReader;
            _filesService = fileService;
            _encryption = encryption;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var scope = await _scopeReader.ReadAsync(Request);

            if (scope is null)
            {
                return Unauthorized();
            }

            Console.WriteLine("Permissions: " + string.Join(", ", scope.Permissions));

            if (!scope.Has("files.write"))
            {
                return StatusCode(
                    403,
                    new { StatusCode = "MISSING_PERMISSIONS" });
            }

            if (file is null || file.Length == 0)
            {
                return BadRequest(new { code = "MISSING_FILE" });
            }

            var result = await _filesService.CreateFileAsync(
                file.FileName,
                file.ContentType,
                file.Length,
                scope.TenantId,
                scope.MemberID);

            // Extract the new file ID from the JSON result
            using var document = System.Text.Json.JsonDocument.Parse(result);
            var fileId = document.RootElement.GetProperty("fileId").GetString();

            if (string.IsNullOrWhiteSpace(fileId))
            {
                return StatusCode(500, new { code = "FILE_ID_NOT_RETURNED" });
            }

            // Read the uploaded file bytes
            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            // Encrypt (AES-256-GCM with a fresh per-file key, wrapped by the KEK)
            var encrypted = _encryption.Encrypt(memoryStream.ToArray());

            // Save the encrypted content and mark the file as uploaded
            await _filesService.SaveFileContentAsync(
                fileId,
                encrypted,
                scope.TenantId,
                scope.MemberID);

            return Ok(new
            {
                fileId,
                fileName = file.FileName,
                sizeBytes = file.Length,
                status = "UPLOADED"
            });
        }
    }
}
