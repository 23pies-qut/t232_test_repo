using JamessProject.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


//"builder" is the object that is building my API application.
//"Builder.Services" is where Services talks to ASP.NET and
//tell it what the application is going to need such as DatabaseService and JWT Authentication."
//"AddScoped" tells ASP.NET how long the application should live for e.g. creates one DatabaseService for each HTTP request
//e.g.ASP.NET creates a DatabaseService for a "GET users" request and when the request is finished so is that instance
//3 layer example: if a user clicks on their own profile the front end may send a request like GET /users/123
//this request will be sent to the API users controller which asks for a DatabaseService. 
//The DatabaseService then calls the stored procedure e.g. GetUserById
//from the SQL server which is sent through Json packages from the database to the API to the front end
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<DatabaseService>();

//if there isnt a JWT key in the configuration, stop the application and tell them the key is required.
var jwtkey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is required.");

//this line tells ASP.NET for authentication, use JWT bearer tokens 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //formats how JWT tokens
    .AddJwtBearer(options =>
    {
        //checks through the token parameter requirements
        options.TokenValidationParameters = new TokenValidationParameters
        {
            //checks who issued the token/who created it
            ValidateIssuer = true,
            //checks who the token was meant for
            ValidateAudience = true,
            //checks token is not expired
            ValidateLifetime = true,
            //checks the secret key is correct
            ValidateIssuerSigningKey = true,

            //tells ASP.NET what issuer we are expecting
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            //tells ASP.NET who the token is intended for
            ValidAudience = builder.Configuration["Jwt:Audience"],

            //the key is stored in normal text, the JWT system needs it as a set of bytes so it uses
            //"Encoding.UTF8.GetBytes(jwtkey))" to convert the text into bytes
            //"new SymmetricSecurityKey" turns the bytes into a cryptographic signing key that JWT can use
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtkey))
        };
    });
//enables authorization system below
builder.Services.AddAuthorization();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//after API controller and JWT validation
//who are you?
app.UseAuthentication();

//are you allowed?
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/database-test", async (DatabaseService database) =>
{
    var connected = await database.TestConnectionAsync();

    return Results.Ok(new
    {
        databaseConnected = connected
    });
});

app.MapControllers();

app.Run();
