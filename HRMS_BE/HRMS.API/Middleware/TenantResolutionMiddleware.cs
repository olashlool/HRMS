using HRMS.Application.Common.Interfaces;
using HRMS.Application.Tenants;
using HRMS.Infrastructure.Authentication;

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
        var slug = ResolveSlug(context, environment);

        if (slug is not null)
        {
            var tenant = await tenantLookup.FindActiveBySlugAsync(slug, context.RequestAborted);

            if (tenant is null)
            {
                _logger.LogWarning("No active tenant matches the slug '{Slug}'.", slug);
            }
            else
            {
                currentTenantSetter.Set(tenant.Id, tenant.Slug, tenant.DatabaseName);
            }
        }

        await _next(context);
    }

    private static string? ResolveSlug(HttpContext context, IWebHostEnvironment environment)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(AccessTokenGenerator.TenantSlugClaim)?.Value;
        }

        if (environment.IsDevelopment()
            && context.Request.Headers.TryGetValue(DevelopmentTenantHeader, out var header))
        {
            return header.ToString();
        }

        return null;
    }
}
