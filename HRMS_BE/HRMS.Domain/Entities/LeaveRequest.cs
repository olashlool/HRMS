using HRMS.Domain.Common;
using HRMS.Domain.Entities.Enums;

namespace HRMS.Domain.Entities;

public sealed class LeaveRequest : AuditableEntity
{
    public const int ReasonMaxLength = 500;
    public const int DecisionNoteMaxLength = 500;

    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int Days { get; private set; }
    public string? Reason { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    public Guid? DecidedByEmployeeId { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? DecisionNote { get; private set; }

    private LeaveRequest()
    {
    }

    private LeaveRequest(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        int days,
        string? reason)
    {
        Id = Guid.CreateVersion7();
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        Days = days;
        Reason = reason;
        Status = LeaveRequestStatus.Pending;
    }

    public static LeaveRequest Submit(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string? reason)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Employee id is required.");
        }

        if (leaveTypeId == Guid.Empty)
        {
            throw new DomainException("Leave type is required.");
        }

        if (startDate == default || endDate == default)
        {
            throw new DomainException("Both a start date and an end date are required.");
        }

        if (endDate < startDate)
        {
            throw new DomainException("The end date cannot be before the start date.");
        }

        var days = CountWorkingDays(startDate, endDate);

        if (days == 0)
        {
            throw new DomainException("The requested period contains no working days.");
        }

        reason = reason?.Trim();

        if (reason is { Length: > ReasonMaxLength })
        {
            reason = reason[..ReasonMaxLength];
        }

        return new LeaveRequest(employeeId, leaveTypeId, startDate, endDate, days, reason);
    }

    public bool Overlaps(DateOnly start, DateOnly end) => StartDate <= end && start <= EndDate;

    public bool BlocksNewRequest => Status is LeaveRequestStatus.Pending or LeaveRequestStatus.Approved;

    /// <summary>
    /// Approving your own leave is forbidden no matter which permissions you hold:
    /// separation of duties is a property of the request, not of the caller's role.
    /// </summary>
    public bool CanBeDecidedBy(Guid approverEmployeeId, bool isDirectManager, bool mayApproveAnyone)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            return false;
        }

        if (approverEmployeeId == EmployeeId)
        {
            return false;
        }

        return isDirectManager || mayApproveAnyone;
    }

    public void Approve(Guid approverEmployeeId, string? note, DateTimeOffset now)
    {
        RequirePending();

        Status = LeaveRequestStatus.Approved;
        DecidedByEmployeeId = approverEmployeeId;
        DecidedAtUtc = now;
        DecisionNote = Truncate(note, DecisionNoteMaxLength);
    }

    public void Reject(Guid approverEmployeeId, string? note, DateTimeOffset now)
    {
        RequirePending();

        Status = LeaveRequestStatus.Rejected;
        DecidedByEmployeeId = approverEmployeeId;
        DecidedAtUtc = now;
        DecisionNote = Truncate(note, DecisionNoteMaxLength);
    }

    public void Cancel(DateOnly today)
    {
        if (Status == LeaveRequestStatus.Cancelled)
        {
            return;
        }

        if (Status == LeaveRequestStatus.Rejected)
        {
            throw new DomainException("A rejected request cannot be cancelled.");
        }

        if (Status == LeaveRequestStatus.Approved && StartDate <= today)
        {
            throw new DomainException("Approved leave that has already started cannot be cancelled.");
        }

        Status = LeaveRequestStatus.Cancelled;
    }

    private void RequirePending()
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException($"A request that is {Status} can no longer be decided.");
        }
    }

    private static int CountWorkingDays(DateOnly start, DateOnly end)
    {
        var days = 0;

        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Friday or DayOfWeek.Saturday))
            {
                days++;
            }
        }

        return days;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        value = value?.Trim();

        return value is { Length: > 0 }
            ? value.Length > maxLength ? value[..maxLength] : value
            : null;
    }
}
