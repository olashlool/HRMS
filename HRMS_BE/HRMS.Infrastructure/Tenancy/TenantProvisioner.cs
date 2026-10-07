using HRMS.Application.Common.Interfaces;
using HRMS.Infrastructure.Persistence.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Tenancy;

public sealed class TenantProvisioner : ITenantProvisioner
{
    private readonly TenantConnectionStringFactory _connectionStringFactory;
    private readonly ILogger<TenantProvisioner> _logger;

    public TenantProvisioner(
        TenantConnectionStringFactory connectionStringFactory,
        ILogger<TenantProvisioner> logger)
    {
        _connectionStringFactory = connectionStringFactory;
        _logger = logger;
    }

    public async Task ProvisionAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        var connectionString = _connectionStringFactory.Create(databaseName);

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new TenantDbContext(options);

        _logger.LogInformation("Provisioning tenant database {DatabaseName}.", databaseName);

        await context.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Tenant database {DatabaseName} is up to date.", databaseName);
    }
}
