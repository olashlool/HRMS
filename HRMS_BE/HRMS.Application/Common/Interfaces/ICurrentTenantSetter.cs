namespace HRMS.Application.Common.Interfaces;

public interface ICurrentTenantSetter
{
    void Set(Guid tenantId, string slug, string databaseName);
}
