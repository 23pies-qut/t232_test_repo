using IdentityKeyApi.Models;
using IdentityKeyApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityKeyApi.Controllers;

[ApiController]
[IgnoreAntiforgeryToken]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private const long MaximumFileSize =
        25 * 1024 * 1024;

    private readonly IFileService _fileService;

    public FilesController(
        IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FileMetadata>>>
        GetFiles()
    {
        List<FileMetadata> files =
            await _fileService.GetFilesAsync();

        return Ok(files);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumFileSize)]
    public async Task<ActionResult<FileUploadResult>>
        UploadFile(
            [FromForm] IFormFile file)
    {
        if (file is null)
        {
            return BadRequest(
                new
                {
                    error = "FILE_REQUIRED",
                    message =
                        "A file must be selected."
                }
            );
        }

        if (file.Length == 0)
        {
            return BadRequest(
                new
                {
                    error = "FILE_EMPTY",
                    message =
                        "The uploaded file is empty."
                }
            );
        }

        if (file.Length > MaximumFileSize)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new
                {
                    error = "FILE_TOO_LARGE",
                    message =
                        "The maximum file size is 25 MB."
                }
            );
        }

        await using MemoryStream memoryStream =
            new();

        await file.CopyToAsync(memoryStream);

        FileUploadResult result =
            await _fileService.UploadFileAsync(
                file.FileName,
                file.ContentType,
                memoryStream.ToArray()
            );

        return CreatedAtAction(
            nameof(DownloadFile),
            new
            {
                id = result.FileId
            },
            result
        );
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(
        string id)
    {
        FileDownloadData? file =
            await _fileService.DownloadFileAsync(id);

        if (file is null)
        {
            return NotFound(
                new
                {
                    error = "FILE_NOT_FOUND",
                    message =
                        "The requested file was not found."
                }
            );
        }

        return File(
            file.Content,
            file.MimeType,
            file.FileName
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(
        string id)
    {
        bool deleted =
            await _fileService.DeleteFileAsync(id);

        if (!deleted)
        {
            return NotFound(
                new
                {
                    error = "FILE_NOT_FOUND",
                    message =
                        "The requested file was not found."
                }
            );
        }

        return Ok(
            new
            {
                fileId = id,
                status = "DELETED"
            }
        );
    }
}
