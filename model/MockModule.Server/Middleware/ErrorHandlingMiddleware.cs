using Microsoft.Data.SqlClient;
using MockModule.Server.Models;

namespace MockModule.Server.Middleware
{
    /// <summary>
    /// Catches exceptions thrown by controllers and services and returns JSON: { code, message, trace }.
    /// Registered in Program.cs: app.UseMiddleware&lt;ErrorHandlingMiddleware&gt;();
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly bool _showDetails;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _showDetails = environment.IsDevelopment(); // in Development the real error message is returned to help debugging
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                (int status, string code, string message, IReadOnlyList<Step>? trace) error = ex switch
                {
                    ApiException a => (a.StatusCode, a.Code, a.Message, a.Trace),
                    SqlException { Number: 50003 } => (404, "FILES_NOT_FOUND", "File not found.", null),
                    SqlException { Number: 50001 } => (500, "FILES_CONTEXT_REQUIRED", "The database session has no tenant set (SESSION_CONTEXT).", null),
                    SqlException { Number: 33504 } => (403, "RLS_BLOCKED", "Row-Level Security blocked a write to another tenant's data.", null),
                    BadHttpRequestException b => (b.StatusCode, "BAD_REQUEST", b.Message, null),
                    _ => (500, "SERVER_ERROR", _showDetails ? $"{ex.GetType().Name}: {ex.Message}" : "Internal server error. See the API log for details.", null)
                };
                if (error.status >= 500)
                    _logger.LogError(ex, "{Method} {Path} failed", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = error.status;
                await context.Response.WriteAsJsonAsync(new { error.code, error.message, error.trace });
            }
        }
    }
}
