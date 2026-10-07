using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Catalog.Configurations;

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.Purpose).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(UserToken.TokenHashLength).IsFixedLength();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).IsRequired();

        builder.HasIndex(t => new { t.UserId, t.Purpose }).HasDatabaseName("IX_UserTokens_UserId_Purpose");
        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("UX_UserTokens_TokenHash");

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode>
{
    public void Configure(EntityTypeBuilder<RecoveryCode> builder)
    {
        builder.ToTable("RecoveryCodes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.CodeHash).IsRequired().HasMaxLength(RecoveryCode.CodeHashLength).IsFixedLength();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => c.UserId).HasDatabaseName("IX_RecoveryCodes_UserId");

        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.AttemptedEmail).IsRequired().HasMaxLength(LoginAttempt.EmailMaxLength);
        builder.Property(a => a.Succeeded).IsRequired();
        builder.Property(a => a.FailureReason).HasMaxLength(LoginAttempt.FailureReasonMaxLength);
        builder.Property(a => a.IpAddress).HasMaxLength(LoginAttempt.IpAddressMaxLength);
        builder.Property(a => a.UserAgent).HasMaxLength(LoginAttempt.UserAgentMaxLength);
        builder.Property(a => a.OccurredAtUtc).IsRequired();

        builder.HasIndex(a => new { a.UserId, a.OccurredAtUtc }).HasDatabaseName("IX_LoginAttempts_UserId_OccurredAtUtc");
    }
}
