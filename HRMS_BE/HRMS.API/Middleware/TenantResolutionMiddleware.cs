using HRMS.Application.Common.Interfaces;
using HRMS.Application.Tenants;

namespace HRMS.API.Middleware;

public sealed class TenantResolutionMiddleware
{
    public const string DevelopmentTenantHeader = "X-Tenant-Slug";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IWebHostEnvironment environment,
        ITenantLookup tenantLookup,
        ICurrentTenantSetter currentTenantSetter)
    {
        if (!environment.IsDevelopment())
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(DevelopmentTenantHeader, out var header))
        {
            await _next(context);
            return;
        }

        var tenant = await tenantLookup.FindActiveBySlugAsync(header.ToString(), context.RequestAborted);

        if (tenant is null)
        {
            _logger.LogWarning("No active tenant matches the development header value '{Slug}'.", header.ToString());
        }
        else
        {
            currentTenantSetter.Set(tenant.Id, tenant.Slug, tenant.DatabaseName);
        }

        await _next(context);
    }
}
