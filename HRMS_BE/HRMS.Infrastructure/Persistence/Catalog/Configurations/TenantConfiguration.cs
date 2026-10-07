using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Catalog.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(Tenant.NameMaxLength);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(Tenant.SlugMaxLength);
        builder.Property(t => t.DatabaseName).IsRequired().HasMaxLength(Tenant.DatabaseNameMaxLength);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(128);
        builder.Property(t => t.ModifiedBy).HasMaxLength(128);
        builder.Property(t => t.DeletedBy).HasMaxLength(128);
        builder.Property(t => t.IsDeleted).IsRequired();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("UX_Tenants_Slug")
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(t => t.DatabaseName).IsUnique().HasDatabaseName("UX_Tenants_DatabaseName")
            .HasFilter("[IsDeleted] = 0");
    }
}
