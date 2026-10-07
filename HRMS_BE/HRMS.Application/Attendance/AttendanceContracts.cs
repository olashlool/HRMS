namespace HRMS.Application.Attendance.Dtos;

public sealed record AttendanceRecordResponse(
    Guid Id,
    Guid EmployeeId,
    DateOnly WorkDate,
    DateTimeOffset? CheckInUtc,
    DateTimeOffset? CheckOutUtc,
    int WorkedMinutes,
    string Status,
    string? Note);

public sealed record MarkAttendanceRequest(Guid EmployeeId, DateOnly WorkDate, int Status, string? Note);
