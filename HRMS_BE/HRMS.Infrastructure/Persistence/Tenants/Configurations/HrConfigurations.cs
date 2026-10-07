using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Tenants.Configurations;

public sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Code).IsRequired().HasMaxLength(LeaveType.CodeMaxLength);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(LeaveType.NameMaxLength);
        builder.Property(t => t.DefaultAnnualDays).IsRequired();
        builder.Property(t => t.IsPaid).IsRequired();
        builder.Property(t => t.RequiresApproval).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(128);
        builder.Property(t => t.ModifiedBy).HasMaxLength(128);
        builder.Property(t => t.DeletedBy).HasMaxLength(128);
        builder.Property(t => t.IsDeleted).IsRequired();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("UX_LeaveTypes_Code").HasFilter("[IsDeleted] = 0");
    }
}

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.StartDate).IsRequired();
        builder.Property(r => r.EndDate).IsRequired();
        builder.Property(r => r.Days).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(LeaveRequest.ReasonMaxLength);
        builder.Property(r => r.DecisionNote).HasMaxLength(LeaveRequest.DecisionNoteMaxLength);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(r => r.CreatedBy).HasMaxLength(128);
        builder.Property(r => r.ModifiedBy).HasMaxLength(128);
        builder.Property(r => r.DeletedBy).HasMaxLength(128);
        builder.Property(r => r.IsDeleted).IsRequired();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => new { r.EmployeeId, r.StartDate })
            .HasDatabaseName("IX_LeaveRequests_EmployeeId_StartDate");

        builder.HasIndex(r => r.Status).HasDatabaseName("IX_LeaveRequests_Status");

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(r => r.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Year).IsRequired();
        builder.Property(b => b.EntitledDays).IsRequired();
        builder.Property(b => b.UsedDays).IsRequired();
        builder.Property(b => b.CreatedBy).HasMaxLength(128);
        builder.Property(b => b.ModifiedBy).HasMaxLength(128);
        builder.Property(b => b.DeletedBy).HasMaxLength(128);
        builder.Property(b => b.IsDeleted).IsRequired();

        builder.Ignore(b => b.RemainingDays);

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.HasIndex(b => new { b.EmployeeId, b.LeaveTypeId, b.Year })
            .IsUnique()
            .HasDatabaseName("UX_LeaveBalances_Employee_Type_Year")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<Employee>().WithMany().HasForeignKey(b => b.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(b => b.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.WorkDate).IsRequired();
        builder.Property(r => r.WorkedMinutes).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(r => r.Note).HasMaxLength(AttendanceRecord.NoteMaxLength);
        builder.Property(r => r.CreatedBy).HasMaxLength(128);
        builder.Property(r => r.ModifiedBy).HasMaxLength(128);
        builder.Property(r => r.DeletedBy).HasMaxLength(128);
        builder.Property(r => r.IsDeleted).IsRequired();

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => new { r.EmployeeId, r.WorkDate })
            .IsUnique()
            .HasDatabaseName("UX_AttendanceRecords_Employee_WorkDate")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<Employee>().WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Cascade);
    }
}
