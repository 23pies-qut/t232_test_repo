using Microsoft.Data.SqlClient;
using JamessProject.API.Models;

namespace JamessProject.API.Data;

public class DatabaseService
{
    private readonly string _connectionString;

    //if there isnt a database connection string in the configuration,
    //stop the application and say the connection string is required
    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("JamesProject")
            ?? throw new InvalidOperationException(
                "Connection string 'JamesProject' is required.");
    }

    //test to see if the API can communicate with the database
    public async Task<bool> TestConnectionAsync()
    {
        //creates a connection to the SQL server using the connection string put in appsettings.json
        await using var connection = new SqlConnection(_connectionString);

        //tries to connect
        await connection.OpenAsync();

        return connection.State == System.Data.ConnectionState.Open;
    }

    //creates a new method that recieves tenantId and eventually returns a string
    //"Guid" is because of the UNIQUEIDENTIFIER parameter on tenantId
    public async Task<string> GetUsersByTenantAsync(Guid tenantId)
    {
        //creates connection database using string from appsettings.json
        await using var connection = new SqlConnection(_connectionString);

        //Opens the connection
        await connection.OpenAsync();

        //says that I am executing the the store procedure "GetUsersByTenant"
        await using var command = new SqlCommand("GetUsersByTenant", connection);

        //makes sure the method is looking for a stored procedure not a query
        command.CommandType = System.Data.CommandType.StoredProcedure;

        //supplies the parameter the store procedure expects
        command.Parameters.AddWithValue("@TenantId", tenantId);

        //runs the stored procedure
        var result = await command.ExecuteScalarAsync();

        //returns the json package to whoever called DataService
        return result?.ToString() ?? "[]";
    }

    public async Task<string> GetUserByIdAsync(int id, Guid tenantId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command =
            new SqlCommand("GetUserById", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var result = await command.ExecuteScalarAsync();

        return result?.ToString();
    }

    public async Task<string> CreateUserAsync(CreateUserRequest request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command =
            new SqlCommand("CreateUser", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@TenantId", request.TenantId);
        command.Parameters.AddWithValue("@MemberId", request.MemberId);
        command.Parameters.AddWithValue("@FirstName", request.FirstName);
        command.Parameters.AddWithValue("@LastName", request.LastName);
        command.Parameters.AddWithValue("@UserEmail", request.UserEmail);
        command.Parameters.AddWithValue("@UserPhone", request.UserPhone);
        command.Parameters.AddWithValue("@UserPasswordHash", request.UserPasswordHash);

        var result = await command.ExecuteScalarAsync();

        return result?.ToString();
    }

    public async Task<string?> UpdateUserAsync(int id, UpdateUserRequests request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command =
            new SqlCommand("UpdateUser", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@TenantId", request.TenantId);
        command.Parameters.AddWithValue("@FirstName", request.FirstName);
        command.Parameters.AddWithValue("@LastName", request.LastName);
        command.Parameters.AddWithValue("@UserEmail", request.UserEmail);
        command.Parameters.AddWithValue("@UserPhone", request.UserPhone);

        var result = await command.ExecuteScalarAsync();

        return result?.ToString();
    }

    public async Task<string?> DeleteUserAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command =
            new SqlCommand("DeleteUser", connection);

        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@Id", id);

        var result = await command.ExecuteScalarAsync();

        return result?.ToString();
    }
}