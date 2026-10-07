using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Catalog.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(RefreshToken.TokenHashLength)
            .IsFixedLength();

        builder.Property(t => t.FamilyId)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.ExpiresAtUtc)
            .IsRequired();

        builder.Property(t => t.RevokedReason)
            .HasMaxLength(RefreshToken.RevokedReasonMaxLength);

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(45);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        builder.HasIndex(t => t.FamilyId)
            .HasDatabaseName("IX_RefreshTokens_FamilyId");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
