using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Catalog.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(Role.NameMaxLength);
        builder.Property(r => r.NormalizedName).IsRequired().HasMaxLength(Role.NameMaxLength);
        builder.Property(r => r.Description).HasMaxLength(Role.DescriptionMaxLength);
        builder.Property(r => r.IsSystemRole).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(128);
        builder.Property(r => r.ModifiedBy).HasMaxLength(128);
        builder.Property(r => r.DeletedBy).HasMaxLength(128);
        builder.Property(r => r.IsDeleted).IsRequired();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })
            .IsUnique()
            .HasDatabaseName("UX_Roles_TenantId_NormalizedName")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Id).ValueGeneratedNever();
        builder.Property(rp => rp.Permission).IsRequired().HasMaxLength(RolePermission.PermissionMaxLength);

        builder.HasIndex(rp => new { rp.RoleId, rp.Permission })
            .IsUnique()
            .HasDatabaseName("UX_RolePermissions_RoleId_Permission");

        builder.HasOne<Role>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).ValueGeneratedNever();
        builder.Property(ur => ur.AssignedAtUtc).IsRequired();

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("UX_UserRoles_UserId_RoleId");

        builder.HasOne<User>().WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Role>().WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");
        builder.HasKey(up => up.Id);
        builder.Property(up => up.Id).ValueGeneratedNever();
        builder.Property(up => up.Permission).IsRequired().HasMaxLength(RolePermission.PermissionMaxLength);
        builder.Property(up => up.IsGranted).IsRequired();
        builder.Property(up => up.AssignedAtUtc).IsRequired();

        builder.HasIndex(up => new { up.UserId, up.Permission })
            .IsUnique()
            .HasDatabaseName("UX_UserPermissions_UserId_Permission");

        builder.HasOne<User>().WithMany().HasForeignKey(up => up.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
