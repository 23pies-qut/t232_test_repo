using JamessProject.API.Data;
using JamessProject.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace JamessProject.API.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _database;

    public UsersController(DatabaseService database)
    {
        _database = database;
    }

    //Creates an API endpoint - GET /users?tenantId=YOUR-TENANT-ID
    [HttpGet]
    //Does not have direct link to the DB just asks DataService to get users for the tenant
    public async Task<IActionResult> GetUsersByTenant(Guid tenantId)
    {
        var result = await _database.GetUsersByTenantAsync(tenantId);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id, Guid tenantId)
    {
        var result = await _database.GetUserByIdAsync(id, tenantId);

        if (result == null)
        {
            return NotFound();
        }

        return Content(result, "application/json");
    }

    //sending data to the API to create something ( in this case a new users information)
    //rather than using a GET to request data from the API 
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        //gets the method I have made in DataService
        var result = await _database.CreateUserAsync(request);

        //sends datbase result back to the front end in JSON format
        return Content(result ?? "{}", "application/json");
    }

    //HttpPut is used to update an existing resource, in this case a user.
    //The "{id}" in the route indicates that the user ID will be provided in the URL, and the UpdateUserRequests object will
    //contain the updated user information.
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequests request)
    {
        var result = await _database.UpdateUserAsync(id, request);

        if (result == null)
        {
            return NotFound();
        }

        return Content(result, "application/json");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _database.DeleteUserAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Content(result, "application/json");
    }
}

