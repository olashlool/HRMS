using HRMS.Domain.Common;
using HRMS.Domain.Entities.Enums;

namespace HRMS.Domain.Entities;

public sealed class AttendanceRecord : AuditableEntity
{
    public const int NoteMaxLength = 300;

    public static readonly TimeOnly DefaultShiftStart = new(9, 0);

    public Guid EmployeeId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public DateTimeOffset? CheckInUtc { get; private set; }
    public DateTimeOffset? CheckOutUtc { get; private set; }
    public int WorkedMinutes { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string? Note { get; private set; }

    private AttendanceRecord()
    {
    }

    private AttendanceRecord(Guid employeeId, DateOnly workDate, AttendanceStatus status)
    {
        Id = Guid.CreateVersion7();
        EmployeeId = employeeId;
        WorkDate = workDate;
        Status = status;
    }

    public static AttendanceRecord CheckIn(Guid employeeId, DateOnly workDate, DateTimeOffset now)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Employee id is required.");
        }

        if (workDate == default)
        {
            throw new DomainException("A work date is required.");
        }

        var record = new AttendanceRecord(employeeId, workDate, AttendanceStatus.Present)
        {
            CheckInUtc = now
        };

        if (TimeOnly.FromDateTime(now.UtcDateTime) > DefaultShiftStart)
        {
            record.Status = AttendanceStatus.Late;
        }

        return record;
    }

    public static AttendanceRecord Mark(
        Guid employeeId,
        DateOnly workDate,
        AttendanceStatus status,
        string? note)
    {
        if (status is AttendanceStatus.Present or AttendanceStatus.Late)
        {
            throw new DomainException("Present and late records are created by checking in.");
        }

        return new AttendanceRecord(employeeId, workDate, status)
        {
            Note = Truncate(note)
        };
    }

    public void CheckOut(DateTimeOffset now)
    {
        if (CheckInUtc is not { } checkIn)
        {
            throw new DomainException("This record has no check-in to close.");
        }

        if (CheckOutUtc is not null)
        {
            throw new DomainException("This record has already been checked out.");
        }

        if (now < checkIn)
        {
            throw new DomainException("The check-out time cannot be before the check-in time.");
        }

        CheckOutUtc = now;
        WorkedMinutes = (int)Math.Round((now - checkIn).TotalMinutes);
    }

    public void Annotate(string? note) => Note = Truncate(note);

    private static string? Truncate(string? value)
    {
        value = value?.Trim();

        return value is { Length: > 0 }
            ? value.Length > NoteMaxLength ? value[..NoteMaxLength] : value
            : null;
    }
}
