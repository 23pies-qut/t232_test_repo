using Microsoft.AspNetCore.Authentication.JwtBearer;
using MockModule.Server.Middleware;
using MockModule.Server.Models;
using MockModule.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// In Development, generates the JWT signing key and the encryption master key on first run (stored in user-secrets)
DevelopmentKeys.EnsureCreated(builder);

// Register controllers, the OpenAPI document and the services

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<UploadProgressService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<FilesService>();

// JWT authentication: validates signature, issuer, audience and lifetime; keeps the original claim names (tenant_id / member_id / permissions)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = TokenService.ValidationParameters(builder.Configuration);
    });

// One policy per permission, e.g. [Authorize(Policy = "files.read")]
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in TokenService.Permissions)
        options.AddPolicy(permission, policy => policy.RequireClaim(CurrentUser.ClaimPermissions, permission));
});

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Request pipeline: static files -> OpenAPI (Development) -> error handling -> authentication -> authorization -> controllers
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
