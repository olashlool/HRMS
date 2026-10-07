using HRMS.API.Authorization;
using HRMS.API.Filters;
using HRMS.Application.Attendance;
using HRMS.Application.Attendance.Dtos;
using HRMS.Application.Leave;
using HRMS.Application.Leave.Dtos;
using HRMS.Domain.Authorization;
using HRMS.Domain.Billing;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/leave")]
[TenantRequired]
[ActiveSubscriptionRequired]
public sealed class LeaveController : ControllerBase
{
    private readonly ILeaveService _leave;

    public LeaveController(ILeaveService leave)
    {
        _leave = leave;
    }

    [HttpGet("types")]
    [HasPermission(Permissions.Leave.Read)]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeResponse>>> Types(CancellationToken cancellationToken)
    {
        return Ok(await _leave.ListTypesAsync(cancellationToken));
    }

    [HttpGet("balances")]
    [HasPermission(Permissions.Leave.Read)]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceResponse>>> Balances(CancellationToken cancellationToken)
    {
        return Ok(await _leave.MyBalancesAsync(cancellationToken));
    }

    [HttpGet("mine")]
    [HasPermission(Permissions.Leave.Read)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestResponse>>> Mine(CancellationToken cancellationToken)
    {
        return Ok(await _leave.ListMineAsync(cancellationToken));
    }

    [HttpPost("requests")]
    [HasPermission(Permissions.Leave.Request)]
    public async Task<ActionResult<LeaveRequestResponse>> Submit(
        SubmitLeaveRequest request,
        CancellationToken cancellationToken)
    {
        return await _leave.SubmitAsync(request, cancellationToken);
    }

    [HttpPost("requests/{id:guid}/cancel")]
    [HasPermission(Permissions.Leave.Request)]
    public async Task<ActionResult<LeaveRequestResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return await _leave.CancelAsync(id, cancellationToken);
    }

    [HttpGet("approvals")]
    [HasPermission(Permissions.Leave.ApproveTeam)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestResponse>>> Approvals(CancellationToken cancellationToken)
    {
        return Ok(await _leave.ListPendingForApprovalAsync(cancellationToken));
    }

    [HttpPost("requests/{id:guid}/decision")]
    [HasPermission(Permissions.Leave.ApproveTeam)]
    public async Task<ActionResult<LeaveRequestResponse>> Decide(
        Guid id,
        LeaveDecisionRequest decision,
        CancellationToken cancellationToken)
    {
        return await _leave.DecideAsync(id, decision, cancellationToken);
    }
}

[ApiController]
[Route("api/attendance")]
[TenantRequired]
[RequiresFeature(Features.Attendance)]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendance;

    public AttendanceController(IAttendanceService attendance)
    {
        _attendance = attendance;
    }

    [HttpPost("check-in")]
    [HasPermission(Permissions.Attendance.Record)]
    public async Task<ActionResult<AttendanceRecordResponse>> CheckIn(CancellationToken cancellationToken)
    {
        return await _attendance.CheckInAsync(cancellationToken);
    }

    [HttpPost("check-out")]
    [HasPermission(Permissions.Attendance.Record)]
    public async Task<ActionResult<AttendanceRecordResponse>> CheckOut(CancellationToken cancellationToken)
    {
        return await _attendance.CheckOutAsync(cancellationToken);
    }

    [HttpGet("mine")]
    [HasPermission(Permissions.Attendance.Read)]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> Mine(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var (start, end) = Range(from, to);

        return Ok(await _attendance.ListMineAsync(start, end, cancellationToken));
    }

    [HttpGet("employees/{employeeId:guid}")]
    [HasPermission(Permissions.Attendance.Read)]
    public async Task<ActionResult<IReadOnlyList<AttendanceRecordResponse>>> ForEmployee(
        Guid employeeId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var (start, end) = Range(from, to);

        return Ok(await _attendance.ListForEmployeeAsync(employeeId, start, end, cancellationToken));
    }

    [HttpPost("mark")]
    [HasPermission(Permissions.Attendance.ManageAny)]
    public async Task<ActionResult<AttendanceRecordResponse>> Mark(
        MarkAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        return await _attendance.MarkAsync(request, cancellationToken);
    }

    private static (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-30);

        return (start, end);
    }
}
