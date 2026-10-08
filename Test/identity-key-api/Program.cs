using Microsoft.AspNetCore.Http.Features;
using IdentityKeyApi.Services;
using IdentityKeyApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ITenantContextService, TenantContextService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddControllers();
builder.Services.AddAntiforgery();
builder.Services.AddOpenApi();

// Keep uploads in memory. Anything above 64 KB (the default threshold) is
// spooled to a temp file, which this machine refuses to create. 30 MB covers
// the 25 MB upload limit, so valid files never touch the disk.
builder.Services.Configure<FormOptions>(options =>
{
    options.MemoryBufferThreshold = 30 * 1024 * 1024;
});

builder.Services.AddCors(options => options.AddPolicy("ReactPolicy",
    policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Fail fast if the encryption key is missing or invalid, instead of
// returning 500 on the first request that needs the service.
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IEncryptionService>();
}

app.UseCors("ReactPolicy");
app.UseMiddleware<TenantContextMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => "Identity Key API Running");
app.MapControllers();

app.Run();
