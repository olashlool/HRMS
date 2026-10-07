using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class LeaveType : AuditableEntity
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 100;

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int DefaultAnnualDays { get; private set; }
    public bool IsPaid { get; private set; }
    public bool RequiresApproval { get; private set; }

    private LeaveType()
    {
    }

    private LeaveType(string code, string name, int defaultAnnualDays, bool isPaid, bool requiresApproval)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        Name = name;
        DefaultAnnualDays = defaultAnnualDays;
        IsPaid = isPaid;
        RequiresApproval = requiresApproval;
    }

    public static LeaveType Create(
        string code,
        string name,
        int defaultAnnualDays,
        bool isPaid = true,
        bool requiresApproval = true)
    {
        code = code?.Trim().ToUpperInvariant() ?? string.Empty;
        name = name?.Trim() ?? string.Empty;

        if (code.Length == 0 || code.Length > CodeMaxLength)
        {
            throw new DomainException("Leave type code is required and must not exceed 32 characters.");
        }

        if (name.Length == 0 || name.Length > NameMaxLength)
        {
            throw new DomainException("Leave type name is required and must not exceed 100 characters.");
        }

        if (defaultAnnualDays is < 0 or > 365)
        {
            throw new DomainException("The default annual allowance must be between 0 and 365 days.");
        }

        return new LeaveType(code, name, defaultAnnualDays, isPaid, requiresApproval);
    }
}

public sealed class LeaveBalance : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public int Year { get; private set; }
    public int EntitledDays { get; private set; }
    public int UsedDays { get; private set; }

    public int RemainingDays => EntitledDays - UsedDays;

    private LeaveBalance()
    {
    }

    private LeaveBalance(Guid employeeId, Guid leaveTypeId, int year, int entitledDays)
    {
        Id = Guid.CreateVersion7();
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        Year = year;
        EntitledDays = entitledDays;
    }

    public static LeaveBalance OpenFor(Guid employeeId, Guid leaveTypeId, int year, int entitledDays)
    {
        if (employeeId == Guid.Empty || leaveTypeId == Guid.Empty)
        {
            throw new DomainException("Employee id and leave type id are required.");
        }

        if (entitledDays < 0)
        {
            throw new DomainException("The entitlement cannot be negative.");
        }

        return new LeaveBalance(employeeId, leaveTypeId, year, entitledDays);
    }

    public void Consume(int days)
    {
        if (days <= 0)
        {
            throw new DomainException("The number of days consumed must be positive.");
        }

        if (days > RemainingDays)
        {
            throw new DomainException(
                $"Only {RemainingDays} day(s) remain for {Year}, but {days} were requested.");
        }

        UsedDays += days;
    }

    public void Release(int days)
    {
        if (days <= 0)
        {
            throw new DomainException("The number of days released must be positive.");
        }

        UsedDays = Math.Max(0, UsedDays - days);
    }
}
