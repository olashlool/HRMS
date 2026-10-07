using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Tenants;

public sealed class TenantLookup : ITenantLookup
{
    private readonly ICatalogDbContext _catalog;

    public TenantLookup(ICatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    public async Task<TenantLookupResult?> FindActiveBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalized = slug?.Trim().ToLowerInvariant() ?? string.Empty;

        if (normalized.Length == 0)
        {
            return null;
        }

        return await _catalog.Tenants
            .AsNoTracking()
            .Where(t => t.Slug == normalized && t.Status == TenantStatus.Active)
            .Select(t => new TenantLookupResult(t.Id, t.Slug, t.DatabaseName))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
