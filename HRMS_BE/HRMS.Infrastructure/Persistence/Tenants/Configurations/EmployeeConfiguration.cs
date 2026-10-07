using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Tenants.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(Employee.EmployeeNumberMaxLength);

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(Employee.NameMaxLength);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(Employee.NameMaxLength);

        builder.Property(e => e.WorkEmail)
            .IsRequired()
            .HasMaxLength(Employee.EmailMaxLength);

        builder.Property(e => e.HireDate)
            .IsRequired();

        builder.Property(e => e.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(128);
        builder.Property(e => e.ModifiedBy).HasMaxLength(128);
        builder.Property(e => e.DeletedBy).HasMaxLength(128);
        builder.Property(e => e.IsDeleted).IsRequired();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.EmployeeNumber)
            .IsUnique()
            .HasDatabaseName("UX_Employees_EmployeeNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(e => e.WorkEmail)
            .IsUnique()
            .HasDatabaseName("UX_Employees_WorkEmail")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_Employees_UserId");
    }
}
