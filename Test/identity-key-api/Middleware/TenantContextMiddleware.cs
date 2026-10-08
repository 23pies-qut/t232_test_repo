using IdentityKeyApi.Services;

namespace IdentityKeyApi.Middleware;

public class TenantContextMiddleware
{
    private const string TenantHeader = "X-Tenant-ID";
    private const string MemberHeader = "X-Member-ID";

    private readonly RequestDelegate _next;

    public TenantContextMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextService tenantContextService)
    {
        if (!context.Request.Path.StartsWithSegments(
                "/api/files"))
        {
            await _next(context);
            return;
        }

        string? tenantId = await TryResolveIdAsync(
            context,
            TenantHeader,
            "TENANT_ID_REQUIRED",
            "The X-Tenant-ID header is required.",
            "TENANT_ID_INVALID",
            "The X-Tenant-ID header is invalid."
        );

        if (tenantId is null)
        {
            return;
        }

        string? memberId = await TryResolveIdAsync(
            context,
            MemberHeader,
            "MEMBER_ID_REQUIRED",
            "The X-Member-ID header is required.",
            "MEMBER_ID_INVALID",
            "The X-Member-ID header is invalid."
        );

        if (memberId is null)
        {
            return;
        }

        tenantContextService.SetContext(
            tenantId,
            memberId
        );

        await _next(context);
    }

    private static async Task<string?> TryResolveIdAsync(
        HttpContext context,
        string headerName,
        string requiredCode,
        string requiredMessage,
        string invalidCode,
        string invalidMessage)
    {
        if (!context.Request.Headers.TryGetValue(
                headerName,
                out var headerValue))
        {
            await WriteErrorResponse(
                context,
                requiredCode,
                requiredMessage
            );

            return null;
        }

        if (!Guid.TryParse(
                headerValue.ToString().Trim(),
                out Guid parsedId))
        {
            await WriteErrorResponse(
                context,
                invalidCode,
                invalidMessage
            );

            return null;
        }

        return parsedId.ToString("N");
    }

    private static async Task WriteErrorResponse(
        HttpContext context,
        string errorCode,
        string message)
    {
        context.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        context.Response.ContentType =
            "application/json";

        await context.Response.WriteAsJsonAsync(
            new
            {
                error = errorCode,
                message
            }
        );
    }
}
