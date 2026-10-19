using System.Security.Cryptography;
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
        private readonly IEncryptionService _encryption;

        public FileDownloadController(
            FilesService filesService,
            ScopeReaderService scopeReader,
            IEncryptionService encryption)
        {
            _filesService = filesService;
            _scopeReader = scopeReader;
            _encryption = encryption;
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

            byte[] plaintext;
            try
            {
                plaintext = _encryption.Decrypt(new EncryptedFileData
                {
                    Ciphertext = file.Ciphertext,
                    Nonce = file.Nonce,
                    AuthTag = file.AuthTag,
                    WrappedDek = file.WrappedDek,
                    PlaintextChecksum = file.PlaintextChecksum
                });
            }
            catch (CryptographicException)
            {
                // Tampered data, wrong KEK, or a row saved before encryption was added
                return StatusCode(500, new { code = "FILE_DECRYPTION_FAILED" });
            }

            return File(plaintext, "application/octet-stream", file.FileName);
        }
    }
}