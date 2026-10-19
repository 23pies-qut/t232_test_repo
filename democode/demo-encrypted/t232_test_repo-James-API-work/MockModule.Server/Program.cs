using MockModule.Server.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Development
});

Console.WriteLine("Environment: " + builder.Environment.EnvironmentName);

var repositoryRoot = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", ".."));

var frontendPath = Path.Combine(repositoryRoot, "File-Upload-Module");

Console.WriteLine("Repository root: " + repositoryRoot);
Console.WriteLine("Frontend path: " + frontendPath);
Console.WriteLine("Frontend index exists: " +
    File.Exists(Path.Combine(frontendPath, "index.html")));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ScopeReaderService>();
builder.Services.AddScoped<FilesService>();
// Reads the KEK once at startup; fails fast if it is missing or invalid.
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

var connectionString = builder.Configuration.GetConnectionString("MockModuleData")
    ?? throw new InvalidOperationException("MockModuleDB connection required.");

var app = builder.Build();

// Serve the frontend from the separate File-Upload-Module folder.
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallback(async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(
        Path.Combine(frontendPath, "index.html"));
});

app.Run();
