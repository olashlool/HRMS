using HRMS.Application.Tenants.Dtos;

namespace HRMS.Application.Tenants;

public interface ITenantService
{
    Task<TenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);

    Task<TenantResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenantResponse> RetryProvisioningAsync(Guid id, CancellationToken cancellationToken = default);
}
