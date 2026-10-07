using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Tenants.Dtos;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Tenants;

public sealed class TenantService : ITenantService
{
    private const string DatabaseNamePrefix = "HRMS_";

    private readonly ICatalogDbContext _catalog;

    public TenantService(ICatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var slugTaken = await _catalog.Tenants.AsNoTracking().AnyAsync(t => t.Slug == slug, cancellationToken);

        if (slugTaken)
            throw new ConflictException($"Slug '{slug}' is already in use.");

        var databaseName = DatabaseNamePrefix + slug.Replace('-', '_');
        var tenant = Tenant.Create(request.Name, slug, databaseName);

        _catalog.Tenants.Add(tenant);

        await _catalog.SaveChangesAsync(cancellationToken);
        return ToResponse(tenant);
    }

    public async Task<TenantResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _catalog.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), id);

        return ToResponse(tenant);
    }

    private static TenantResponse ToResponse(Tenant tenant) =>
        new(tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Status.ToString(),
            tenant.CreatedAtUtc);
}
