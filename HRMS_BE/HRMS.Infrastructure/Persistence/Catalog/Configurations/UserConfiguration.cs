using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Catalog.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.TenantId)
            .IsRequired();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(User.EmailMaxLength);

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(User.EmailMaxLength);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(User.FullNameMaxLength);

        builder.Property(u => u.SecurityStamp)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(u => u.EmailConfirmed)
            .IsRequired();

        builder.Property(u => u.AccessFailedCount)
            .IsRequired();

        builder.Property(u => u.TwoFactorEnabled).IsRequired();
        builder.Property(u => u.TwoFactorSecret).HasMaxLength(512);

        builder.Property(u => u.CreatedBy).HasMaxLength(128);
        builder.Property(u => u.ModifiedBy).HasMaxLength(128);
        builder.Property(u => u.DeletedBy).HasMaxLength(128);
        builder.Property(u => u.IsDeleted).IsRequired();

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedEmail")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_Users_TenantId");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
