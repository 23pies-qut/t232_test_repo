using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MockModule.Server.Services;

namespace MockModule.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetFilesListController : ControllerBase
    {
        //Retrieve the list of files for a specific tenant and member, based on the user's scope and permissions.
        private readonly FilesService _filesService;
        //Validates the token and reads the user's scope from the request, which includes the tenantId, memberId, and permissions.
        private readonly ScopeReaderService _scopeReader;

        public GetFilesListController(FilesService filesService, ScopeReaderService scopeReader)
        {
            _filesService = filesService;
            _scopeReader = scopeReader;
        }

        [HttpGet]
        //calling the ListFilesAsync method of the FilesService class to retrieve a list of files for a specific tenant and member.
        //The tenantId and memberId are passed as query parameters in the request URL. 
        public async Task<IActionResult> GetFilesList()
        {
            //Validate the token and read the user's scope
            var scope = await _scopeReader.ReadAsync(Request);

            if (scope is null)
            {
                return Unauthorized();
            }

            //Checking the user's permission
            if (!scope.Has("files.read"))
            {
                return StatusCode(
                    403,
                    new { StatusCode = "MISSING_PERMISSIONS" });
            }

            //Retrieve files for the validated tenant and member
            var files = await _filesService.ListFilesAsync(
                scope.TenantId,
                scope.MemberID);

            //Return the file list as JSON
            return Content(files, "application/json");
        }
            
    }
}
