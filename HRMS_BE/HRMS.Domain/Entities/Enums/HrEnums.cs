namespace HRMS.Domain.Entities.Enums;

public enum LeaveRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum AttendanceStatus
{
    Present = 1,
    Late = 2,
    Absent = 3,
    OnLeave = 4,
    Holiday = 5
}
