using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Leave.Dtos;
using HRMS.Domain.Authorization;
using HRMS.Domain.Common;
using HRMS.Domain.Entities;
using HRMS.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Leave;

public sealed class LeaveService : ILeaveService
{
    private readonly ITenantDbContext _tenantDb;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionResolver _permissions;
    private readonly TimeProvider _timeProvider;

    public LeaveService(
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

    public async Task<IReadOnlyList<LeaveTypeResponse>> ListTypesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultTypesAsync(cancellationToken);

        return await _tenantDb.LeaveTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new LeaveTypeResponse(t.Id, t.Code, t.Name, t.DefaultAnnualDays, t.IsPaid, t.RequiresApproval))
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveRequestResponse> SubmitAsync(
        SubmitLeaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var employee = await RequireLinkedEmployeeAsync(cancellationToken);
        var leaveType = await RequireLeaveTypeAsync(request.LeaveTypeId, cancellationToken);

        var leaveRequest = LeaveRequest.Submit(
            employee.Id, leaveType.Id, request.StartDate, request.EndDate, request.Reason);

        var clashes = await _tenantDb.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employee.Id
                        && (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved)
                        && r.StartDate <= request.EndDate
                        && request.StartDate <= r.EndDate)
            .AnyAsync(cancellationToken);

        if (clashes)
        {
            throw new ConflictException("This period overlaps an existing leave request.");
        }

        var balance = await RequireBalanceAsync(employee.Id, leaveType, request.StartDate.Year, cancellationToken);

        balance.Consume(leaveRequest.Days);

        _tenantDb.LeaveRequests.Add(leaveRequest);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(leaveRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequestResponse>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var employee = await RequireLinkedEmployeeAsync(cancellationToken);

        return await ProjectAsync(
            _tenantDb.LeaveRequests.Where(r => r.EmployeeId == employee.Id), cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequestResponse>> ListPendingForApprovalAsync(
        CancellationToken cancellationToken = default)
    {
        var approver = await RequireLinkedEmployeeAsync(cancellationToken);
        var granted = await _permissions.ResolveAsync(CurrentUserId(), cancellationToken);

        var query = _tenantDb.LeaveRequests.Where(r => r.Status == LeaveRequestStatus.Pending);

        if (!granted.Contains(Permissions.Leave.ApproveAny))
        {
            var reportIds = await _tenantDb.Employees
                .AsNoTracking()
                .Where(e => e.ManagerId == approver.Id)
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(r => reportIds.Contains(r.EmployeeId));
        }

        return await ProjectAsync(query.Where(r => r.EmployeeId != approver.Id), cancellationToken);
    }

    public async Task<LeaveRequestResponse> DecideAsync(
        Guid requestId,
        LeaveDecisionRequest decision,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var approver = await RequireLinkedEmployeeAsync(cancellationToken);

        var leaveRequest = await _tenantDb.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (leaveRequest is null)
        {
            throw new NotFoundException(nameof(LeaveRequest), requestId);
        }

        var subject = await _tenantDb.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == leaveRequest.EmployeeId, cancellationToken);

        var granted = await _permissions.ResolveAsync(CurrentUserId(), cancellationToken);

        var isDirectManager = subject?.ManagerId == approver.Id;
        var mayApproveAnyone = granted.Contains(Permissions.Leave.ApproveAny);

        if (!leaveRequest.CanBeDecidedBy(approver.Id, isDirectManager, mayApproveAnyone))
        {
            throw new ForbiddenException(
                leaveRequest.EmployeeId == approver.Id
                    ? "You cannot decide on your own leave request."
                    : "You may only decide on leave for your direct reports.");
        }

        if (decision.Approve)
        {
            leaveRequest.Approve(approver.Id, decision.Note, now);
        }
        else
        {
            leaveRequest.Reject(approver.Id, decision.Note, now);

            var balance = await _tenantDb.LeaveBalances
                .FirstOrDefaultAsync(
                    b => b.EmployeeId == leaveRequest.EmployeeId
                         && b.LeaveTypeId == leaveRequest.LeaveTypeId
                         && b.Year == leaveRequest.StartDate.Year,
                    cancellationToken);

            balance?.Release(leaveRequest.Days);
        }

        await _tenantDb.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(leaveRequest, cancellationToken);
    }

    public async Task<LeaveRequestResponse> CancelAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var employee = await RequireLinkedEmployeeAsync(cancellationToken);

        var leaveRequest = await _tenantDb.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (leaveRequest is null)
        {
            throw new NotFoundException(nameof(LeaveRequest), requestId);
        }

        if (leaveRequest.EmployeeId != employee.Id)
        {
            throw new ForbiddenException("You may only cancel your own leave requests.");
        }

        var wasReserved = leaveRequest.BlocksNewRequest;

        leaveRequest.Cancel(DateOnly.FromDateTime(now.UtcDateTime));

        if (wasReserved)
        {
            var balance = await _tenantDb.LeaveBalances
                .FirstOrDefaultAsync(
                    b => b.EmployeeId == leaveRequest.EmployeeId
                         && b.LeaveTypeId == leaveRequest.LeaveTypeId
                         && b.Year == leaveRequest.StartDate.Year,
                    cancellationToken);

            balance?.Release(leaveRequest.Days);
        }

        await _tenantDb.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(leaveRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveBalanceResponse>> MyBalancesAsync(CancellationToken cancellationToken = default)
    {
        var employee = await RequireLinkedEmployeeAsync(cancellationToken);
        var year = _timeProvider.GetUtcNow().Year;

        await EnsureDefaultTypesAsync(cancellationToken);

        var types = await _tenantDb.LeaveTypes.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var type in types)
        {
            await RequireBalanceAsync(employee.Id, type, year, cancellationToken);
        }

        await _tenantDb.SaveChangesAsync(cancellationToken);

        var rows = await _tenantDb.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employee.Id && b.Year == year)
            .Join(
                _tenantDb.LeaveTypes.AsNoTracking(),
                b => b.LeaveTypeId,
                t => t.Id,
                (b, t) => new { t.Code, t.Name, b.Year, b.EntitledDays, b.UsedDays })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new LeaveBalanceResponse(
                x.Code, x.Name, x.Year, x.EntitledDays, x.UsedDays, x.EntitledDays - x.UsedDays))
            .ToList();
    }

    private async Task<Employee> RequireLinkedEmployeeAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();

        var employee = await _tenantDb.Employees
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        return employee ?? throw new ForbiddenException(
            "Your account is not linked to an employee record, so leave cannot be managed for you.");
    }

    private Guid CurrentUserId() =>
        Guid.TryParse(_currentUser.Id, out var id)
            ? id
            : throw new ForbiddenException("No authenticated user.");

    private async Task<LeaveType> RequireLeaveTypeAsync(Guid leaveTypeId, CancellationToken cancellationToken)
    {
        await EnsureDefaultTypesAsync(cancellationToken);

        var type = await _tenantDb.LeaveTypes
            .FirstOrDefaultAsync(t => t.Id == leaveTypeId, cancellationToken);

        return type ?? throw new NotFoundException(nameof(LeaveType), leaveTypeId);
    }

    private async Task<LeaveBalance> RequireBalanceAsync(
        Guid employeeId,
        LeaveType type,
        int year,
        CancellationToken cancellationToken)
    {
        var balance = await _tenantDb.LeaveBalances
            .FirstOrDefaultAsync(
                b => b.EmployeeId == employeeId && b.LeaveTypeId == type.Id && b.Year == year,
                cancellationToken);

        if (balance is not null)
        {
            return balance;
        }

        balance = LeaveBalance.OpenFor(employeeId, type.Id, year, type.DefaultAnnualDays);
        _tenantDb.LeaveBalances.Add(balance);

        return balance;
    }

    private async Task EnsureDefaultTypesAsync(CancellationToken cancellationToken)
    {
        if (await _tenantDb.LeaveTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        _tenantDb.LeaveTypes.Add(LeaveType.Create("ANNUAL", "Annual leave", 21));
        _tenantDb.LeaveTypes.Add(LeaveType.Create("SICK", "Sick leave", 14));
        _tenantDb.LeaveTypes.Add(LeaveType.Create("UNPAID", "Unpaid leave", 30, isPaid: false));

        await _tenantDb.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LeaveRequestResponse>> ProjectAsync(
        IQueryable<LeaveRequest> query,
        CancellationToken cancellationToken)
    {
        return await query
            .AsNoTracking()
            .OrderByDescending(r => r.StartDate)
            .Join(
                _tenantDb.LeaveTypes.AsNoTracking(),
                r => r.LeaveTypeId,
                t => t.Id,
                (r, t) => new LeaveRequestResponse(
                    r.Id, r.EmployeeId, t.Code, t.Name, r.StartDate, r.EndDate, r.Days,
                    r.Reason, r.Status.ToString(), r.DecidedByEmployeeId, r.DecidedAtUtc, r.DecisionNote))
            .ToListAsync(cancellationToken);
    }

    private async Task<LeaveRequestResponse> ToResponseAsync(
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var type = await _tenantDb.LeaveTypes
            .AsNoTracking()
            .FirstAsync(t => t.Id == request.LeaveTypeId, cancellationToken);

        return new LeaveRequestResponse(
            request.Id, request.EmployeeId, type.Code, type.Name,
            request.StartDate, request.EndDate, request.Days, request.Reason,
            request.Status.ToString(), request.DecidedByEmployeeId, request.DecidedAtUtc, request.DecisionNote);
    }
}
