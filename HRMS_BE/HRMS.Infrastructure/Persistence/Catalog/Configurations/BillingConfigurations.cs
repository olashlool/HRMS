using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Catalog.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Code).IsRequired().HasMaxLength(Plan.CodeMaxLength);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(Plan.NameMaxLength);
        builder.Property(p => p.Description).HasMaxLength(400);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(p => p.MonthlyPriceMinor).IsRequired();
        builder.Property(p => p.YearlyPriceMinor).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(128);
        builder.Property(p => p.ModifiedBy).HasMaxLength(128);
        builder.Property(p => p.DeletedBy).HasMaxLength(128);
        builder.Property(p => p.IsDeleted).IsRequired();

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("UX_Plans_Code").HasFilter("[IsDeleted] = 0");
    }
}

public sealed class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("PlanFeatures");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Feature).IsRequired().HasMaxLength(PlanFeature.FeatureMaxLength);

        builder.HasIndex(f => new { f.PlanId, f.Feature })
            .IsUnique()
            .HasDatabaseName("UX_PlanFeatures_PlanId_Feature");

        builder.HasOne<Plan>().WithMany().HasForeignKey(f => f.PlanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.BillingCycle).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.StartedAtUtc).IsRequired();
        builder.Property(s => s.CurrentPeriodEndUtc).IsRequired();
        builder.Property(s => s.ExternalSubscriptionId).HasMaxLength(Subscription.ExternalIdMaxLength);
        builder.Property(s => s.CreatedBy).HasMaxLength(128);
        builder.Property(s => s.ModifiedBy).HasMaxLength(128);
        builder.Property(s => s.DeletedBy).HasMaxLength(128);
        builder.Property(s => s.IsDeleted).IsRequired();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => new { s.TenantId, s.StartedAtUtc }).HasDatabaseName("IX_Subscriptions_TenantId_StartedAtUtc");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Plan>().WithMany().HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Number).IsRequired().HasMaxLength(Invoice.NumberMaxLength);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(i => i.AmountMinor).IsRequired();
        builder.Property(i => i.IssuedAtUtc).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(128);
        builder.Property(i => i.ModifiedBy).HasMaxLength(128);
        builder.Property(i => i.DeletedBy).HasMaxLength(128);
        builder.Property(i => i.IsDeleted).IsRequired();

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasIndex(i => i.Number).IsUnique().HasDatabaseName("UX_Invoices_Number").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(i => i.TenantId).HasDatabaseName("IX_Invoices_TenantId");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
