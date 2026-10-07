using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Common.Interfaces;

public interface ITenantDbContext
{
    DbSet<Employee> Employees { get; }

    DbSet<LeaveType> LeaveTypes { get; }

    DbSet<LeaveRequest> LeaveRequests { get; }

    DbSet<LeaveBalance> LeaveBalances { get; }

    DbSet<AttendanceRecord> AttendanceRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
