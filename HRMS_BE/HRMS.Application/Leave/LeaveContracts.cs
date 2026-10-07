namespace HRMS.Application.Leave.Dtos;

public sealed record LeaveTypeResponse(
    Guid Id,
    string Code,
    string Name,
    int DefaultAnnualDays,
    bool IsPaid,
    bool RequiresApproval);

public sealed record SubmitLeaveRequest(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Reason);

public sealed record LeaveDecisionRequest(bool Approve, string? Note);

public sealed record LeaveRequestResponse(
    Guid Id,
    Guid EmployeeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Days,
    string? Reason,
    string Status,
    Guid? DecidedByEmployeeId,
    DateTimeOffset? DecidedAtUtc,
    string? DecisionNote);

public sealed record LeaveBalanceResponse(
    string LeaveTypeCode,
    string LeaveTypeName,
    int Year,
    int EntitledDays,
    int UsedDays,
    int RemainingDays);
