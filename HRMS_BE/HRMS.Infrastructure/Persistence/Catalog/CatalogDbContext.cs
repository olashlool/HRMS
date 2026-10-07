using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence.Catalog.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Persistence.Catalog;

public sealed class CatalogDbContext : SqlServerDbContext, ICatalogDbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<UserToken> UserTokens => Set<UserToken>();

    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();

    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CatalogDbContext).Assembly,
            type => type.Namespace == typeof(TenantConfiguration).Namespace);

        base.OnModelCreating(modelBuilder);
    }
}
