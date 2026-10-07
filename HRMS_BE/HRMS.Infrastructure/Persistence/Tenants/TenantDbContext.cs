using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence.Tenants.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Persistence.Tenants;

public sealed class TenantDbContext : SqlServerDbContext, ITenantDbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TenantDbContext).Assembly,
            type => type.Namespace == typeof(EmployeeConfiguration).Namespace);

        base.OnModelCreating(modelBuilder);
    }
}
