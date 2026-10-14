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

        public FileDeleteController(FilesService filesService)
        {
            _filesService = filesService;
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteFile(string fileId,
            [FromQuery] string tenantId,
            [FromQuery] string memberId)
        {
            var result = await _filesService.DeleteFileAsync(fileId, tenantId, memberId);

            return Ok(result);
        }

       
        
    }
}
