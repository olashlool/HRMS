using HRMS.Application.Billing;

namespace HRMS.Application.Common.Interfaces;

public interface IEntitlementResolver
{
    Task<TenantEntitlements> ResolveAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
