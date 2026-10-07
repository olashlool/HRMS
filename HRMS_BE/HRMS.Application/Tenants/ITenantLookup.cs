namespace HRMS.Application.Tenants;

public interface ITenantLookup
{
    Task<TenantLookupResult?> FindActiveBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
