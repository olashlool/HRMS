namespace HRMS.Application.Common.Interfaces;

public interface ITenantProvisioner
{
    Task ProvisionAsync(string databaseName, CancellationToken cancellationToken = default);
}
