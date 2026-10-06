using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Models;
using MockModule.Server.Services;

namespace MockModule.Server.Controllers
{
    /// <summary>
    /// File endpoints. Every action requires a JWT that carries the matching permission (files.read / upload / delete).
    /// The tenant comes from the token; the database RLS policy filters by tenant again.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly FilesService _filesService;

        public FilesController(FilesService filesService)
        {
            _filesService = filesService;
        }

        // GET api/Files - files of the current tenant
        [HttpGet]
        [Authorize(Policy = TokenService.FilesRead)]
        public async Task<ActionResult<List<FileRow>>> List() =>
            Ok(await _filesService.ListAsync(CurrentUser.From(User)));

        // POST api/Files - multipart/form-data with a field named "file"; the X-Upload-Id header is used to query progress
        [HttpPost]
        [Authorize(Policy = TokenService.FilesUpload)]
        [RequestSizeLimit(FilesService.MaxBytes + 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = FilesService.MaxBytes + 1024 * 1024)]
        public async Task<ActionResult<UploadResponse>> Upload(IFormFile? file, [FromHeader(Name = "X-Upload-Id")] string? uploadId) =>
            Ok(await _filesService.UploadAsync(CurrentUser.From(User), file, uploadId));

        // GET api/Files/progress/{uploadId} - steps an in-progress upload has completed (the frontend uses it to detect a stall)
        [HttpGet("progress/{uploadId}")]
        [Authorize(Policy = TokenService.FilesUpload)]
        public ActionResult<IReadOnlyList<Step>> Progress(string uploadId) =>
            Ok(_filesService.GetUploadProgress(CurrentUser.From(User), uploadId));

        // GET api/Files/{id} - the decrypted file
        [HttpGet("{id}")]
        [Authorize(Policy = TokenService.FilesRead)]
        public async Task<IActionResult> Download(string id)
        {
            var file = await _filesService.DownloadAsync(CurrentUser.From(User), id);
            return File(file.Content, file.MimeType, file.Filename);
        }

        // DELETE api/Files/{id} - soft delete
        [HttpDelete("{id}")]
        [Authorize(Policy = TokenService.FilesDelete)]
        public async Task<IActionResult> Delete(string id)
        {
            await _filesService.DeleteAsync(CurrentUser.From(User), id);
            return NoContent();
        }
    }
}
