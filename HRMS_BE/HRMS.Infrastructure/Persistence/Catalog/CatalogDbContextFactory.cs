using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HRMS.Infrastructure.Persistence.Catalog;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    // LocalDB with Windows authentication: no credentials appear in this
    // string, and it is only ever used to shape a migration, never to serve a
    // request. Override it when generating against a different server.
    private const string DesignTimeConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=HRMS_Catalog;Trusted_Connection=True;TrustServerCertificate=True";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("HRMS_CATALOG_CONNECTION")
            ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new CatalogDbContext(options);
    }
}
