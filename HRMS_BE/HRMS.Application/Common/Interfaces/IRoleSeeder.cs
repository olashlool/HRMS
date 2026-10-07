namespace HRMS.Application.Common.Interfaces;

public interface IRoleSeeder
{
    Task<Guid> EnsureTenantRolesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
