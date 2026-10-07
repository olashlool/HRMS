using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Tenants.Dtos;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Application.Tenants;

public sealed class TenantService : ITenantService
{
    private const string DatabaseNamePrefix = "HRMS_";

    private readonly ICatalogDbContext _catalog;
    private readonly ITenantProvisioner _provisioner;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        ICatalogDbContext catalog,
        ITenantProvisioner provisioner,
        ILogger<TenantService> logger)
    {
        _catalog = catalog;
        _provisioner = provisioner;
        _logger = logger;
    }

    public async Task<TenantResponse> CreateAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;

        var slugTaken = await _catalog.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Slug == slug, cancellationToken);

        if (slugTaken)
        {
            throw new ConflictException($"Slug '{slug}' is already in use.");
        }

        var databaseName = DatabaseNamePrefix + slug.Replace('-', '_');
        var tenant = Tenant.Create(request.Name, slug, databaseName);

        _catalog.Tenants.Add(tenant);
        await _catalog.SaveChangesAsync(cancellationToken);

        await ProvisionAndRecordAsync(tenant, cancellationToken);

        return ToResponse(tenant);
    }

    public async Task<TenantResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _catalog.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), id);
        }

        return ToResponse(tenant);
    }

    public async Task<TenantResponse> RetryProvisioningAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _catalog.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), id);
        }

        await ProvisionAndRecordAsync(tenant, cancellationToken);

        return ToResponse(tenant);
    }

    private async Task ProvisionAndRecordAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        try
        {
            await _provisioner.ProvisionAsync(tenant.DatabaseName, cancellationToken);
            tenant.MarkProvisioned();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provisioning failed for tenant {Slug}.", tenant.Slug);

            tenant.MarkProvisioningFailed();
            await _catalog.SaveChangesAsync(CancellationToken.None);

            throw;
        }

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    private static TenantResponse ToResponse(Tenant tenant) =>
        new(tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Status.ToString(),
            tenant.CreatedAtUtc);
}
