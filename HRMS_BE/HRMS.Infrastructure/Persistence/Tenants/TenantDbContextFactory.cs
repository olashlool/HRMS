using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRMS.Infrastructure.Persistence.Tenants;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    private const string DesignTimeConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=HRMS_DesignTime;Trusted_Connection=True;TrustServerCertificate=True";

    public TenantDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("HRMS_TENANT_CONNECTION")
            ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TenantDbContext(options);
    }
}
