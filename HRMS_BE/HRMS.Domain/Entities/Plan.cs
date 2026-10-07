using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class Plan : AuditableEntity
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 100;

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public long MonthlyPriceMinor { get; private set; }
    public long YearlyPriceMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public int? MaxEmployees { get; private set; }
    public bool IsActive { get; private set; }

    private Plan()
    {
    }

    private Plan(
        string code,
        string name,
        string? description,
        long monthlyPriceMinor,
        long yearlyPriceMinor,
        string currency,
        int? maxEmployees)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        Name = name;
        Description = description;
        MonthlyPriceMinor = monthlyPriceMinor;
        YearlyPriceMinor = yearlyPriceMinor;
        Currency = currency;
        MaxEmployees = maxEmployees;
        IsActive = true;
    }

    public static Plan Create(
        string code,
        string name,
        string? description,
        long monthlyPriceMinor,
        long yearlyPriceMinor,
        string currency,
        int? maxEmployees)
    {
        code = code?.Trim().ToLowerInvariant() ?? string.Empty;
        name = name?.Trim() ?? string.Empty;
        currency = currency?.Trim().ToUpperInvariant() ?? string.Empty;

        if (code.Length == 0 || code.Length > CodeMaxLength)
        {
            throw new DomainException("Plan code is required and must not exceed 32 characters.");
        }

        if (name.Length == 0 || name.Length > NameMaxLength)
        {
            throw new DomainException("Plan name is required and must not exceed 100 characters.");
        }

        if (currency.Length != 3)
        {
            throw new DomainException("Currency must be a three letter ISO 4217 code.");
        }

        if (monthlyPriceMinor < 0 || yearlyPriceMinor < 0)
        {
            throw new DomainException("Prices cannot be negative.");
        }

        if (maxEmployees is <= 0)
        {
            throw new DomainException("The employee limit must be positive, or null for unlimited.");
        }

        return new Plan(code, name, description, monthlyPriceMinor, yearlyPriceMinor, currency, maxEmployees);
    }

    public long PriceFor(Enums.BillingCycle cycle) =>
        cycle == Enums.BillingCycle.Yearly ? YearlyPriceMinor : MonthlyPriceMinor;

    public void Deactivate() => IsActive = false;
}
