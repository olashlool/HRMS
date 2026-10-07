namespace HRMS.Application.Common.Interfaces;

public interface ICurrentTenant
{
    bool IsResolved { get; }

    Guid TenantId { get; }

    string Slug { get; }

    string DatabaseName { get; }
}
