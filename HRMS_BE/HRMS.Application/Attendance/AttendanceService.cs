using HRMS.Application.Attendance.Dtos;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Authorization;
using HRMS.Domain.Entities;
using HRMS.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Attendance;

public interface IAttendanceService
{
    Task<AttendanceRecordResponse> CheckInAsync(CancellationToken cancellationToken = default);

    Task<AttendanceRecordResponse> CheckOutAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecordResponse>> ListMineAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecordResponse>> ListForEmployeeAsync(Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<AttendanceRecordResponse> MarkAsync(MarkAttendanceRequest request, CancellationToken cancellationToken = default);
}

public sealed class AttendanceService : IAttendanceService
{
    private readonly ITenantDbContext _tenantDb;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionResolver _permissions;
    private readonly TimeProvider _timeProvider;

    public AttendanceService(
        ITenantDbContext tenantDb,
        ICurrentUser currentUser,
        IPermissionResolver permissions,
        TimeProvider timeProvider)
    {
        _tenantDb = tenantDb;
        _currentUser = currentUser;
        _permissions = permissions;
        _timeProvider = timeProvider;
    }

    public async Task<AttendanceRecordResponse> CheckInAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var employee = await RequireLinkedEmployeeAsync(cancellationToken);

        var existing = await _tenantDb.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employee.Id && r.WorkDate == today, cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException("You have already checked in today.");
        }

        var record = AttendanceRecord.CheckIn(employee.Id, today, now);

        _tenantDb.AttendanceRecords.Add(record);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        return ToResponse(record);
    }

    public async Task<AttendanceRecordResponse> CheckOutAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var employee = await RequireLinkedEmployeeAsync(cancellationToken);

        var record = await _tenantDb.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employee.Id && r.WorkDate == today, cancellationToken);

        if (record is null)
        {
            throw new NotFoundException(nameof(AttendanceRecord), today);
        }

        record.CheckOut(now);

        await _tenantDb.SaveChangesAsync(cancellationToken);

        return ToResponse(record);
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> ListMineAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var employee = await RequireLinkedEmployeeAsync(cancellationToken);

        return await QueryAsync(employee.Id, from, to, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceRecordResponse>> ListForEmployeeAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var viewer = await RequireLinkedEmployeeAsync(cancellationToken);

        if (employeeId == viewer.Id)
        {
            return await QueryAsync(employeeId, from, to, cancellationToken);
        }

        var granted = await _permissions.ResolveAsync(CurrentUserId(), cancellationToken);

        if (!granted.Contains(Permissions.Attendance.ManageAny))
        {
            var isReport = await _tenantDb.Employees
                .AsNoTracking()
                .AnyAsync(e => e.Id == employeeId && e.ManagerId == viewer.Id, cancellationToken);

            if (!isReport)
            {
                throw new ForbiddenException("You may only view attendance for yourself or your direct reports.");
            }
        }

        return await QueryAsync(employeeId, from, to, cancellationToken);
    }

    public async Task<AttendanceRecordResponse> MarkAsync(
        MarkAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _tenantDb.AttendanceRecords
            .FirstOrDefaultAsync(
                r => r.EmployeeId == request.EmployeeId && r.WorkDate == request.WorkDate,
                cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException("An attendance record already exists for that employee and date.");
        }

        var record = AttendanceRecord.Mark(
            request.EmployeeId, request.WorkDate, (AttendanceStatus)request.Status, request.Note);

        _tenantDb.AttendanceRecords.Add(record);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        return ToResponse(record);
    }

    private async Task<IReadOnlyList<AttendanceRecordResponse>> QueryAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        return await _tenantDb.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= from && r.WorkDate <= to)
            .OrderByDescending(r => r.WorkDate)
            .Select(r => new AttendanceRecordResponse(
                r.Id, r.EmployeeId, r.WorkDate, r.CheckInUtc, r.CheckOutUtc,
                r.WorkedMinutes, r.Status.ToString(), r.Note))
            .ToListAsync(cancellationToken);
    }

    private async Task<Employee> RequireLinkedEmployeeAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();

        var employee = await _tenantDb.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        return employee ?? throw new ForbiddenException(
            "Your account is not linked to an employee record, so attendance cannot be recorded for you.");
    }

    private Guid CurrentUserId() =>
        Guid.TryParse(_currentUser.Id, out var id)
            ? id
            : throw new ForbiddenException("No authenticated user.");

    private static AttendanceRecordResponse ToResponse(AttendanceRecord record) =>
        new(record.Id, record.EmployeeId, record.WorkDate, record.CheckInUtc, record.CheckOutUtc,
            record.WorkedMinutes, record.Status.ToString(), record.Note);
}
