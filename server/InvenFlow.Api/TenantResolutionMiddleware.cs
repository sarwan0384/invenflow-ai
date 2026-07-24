using InvenFlow.Api.Services;
using System.Security.Claims;

namespace InvenFlow.Api;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        var tenantClaim = context.User.FindFirst("tenantId")?.Value
            ?? context.User.FindFirst("TenantId")?.Value;

        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            tenantService.CurrentTenantId = tenantId;
        }
        else
        {
            _logger.LogWarning(
                "Tenant claim was missing or could not be parsed. Expected 'tenantId' or 'TenantId'. User='{UserName}'. Claims='{Claims}'",
                context.User.Identity?.Name ?? "unknown",
                string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}")));

            tenantService.CurrentTenantId = null;
        }

        await _next(context);
    }
}
