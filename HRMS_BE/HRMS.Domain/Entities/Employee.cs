using System.Text.RegularExpressions;
using HRMS.Domain.Common;
using HRMS.Domain.Entities.Enums;

namespace HRMS.Domain.Entities;

public sealed partial class Employee : AuditableEntity
{
    public const int EmployeeNumberMaxLength = 32;
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 256;

    public string EmployeeNumber { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string WorkEmail { get; private set; } = null!;
    public DateOnly HireDate { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? ManagerId { get; private set; }
    private Employee()
    {
    }

    private Employee(
        string employeeNumber,
        string firstName,
        string lastName,
        string workEmail,
        DateOnly hireDate,
        EmploymentType employmentType)
    {
        Id = Guid.CreateVersion7();
        EmployeeNumber = employeeNumber;
        FirstName = firstName;
        LastName = lastName;
        WorkEmail = workEmail;
        HireDate = hireDate;
        EmploymentType = employmentType;
    }

    public static Employee Create(
        string employeeNumber,
        string firstName,
        string lastName,
        string workEmail,
        DateOnly hireDate,
        EmploymentType employmentType)
    {
        employeeNumber = Require(employeeNumber, "Employee number", EmployeeNumberMaxLength).ToUpperInvariant();
        firstName = Require(firstName, "First name", NameMaxLength);
        lastName = Require(lastName, "Last name", NameMaxLength);
        workEmail = Require(workEmail, "Work email", EmailMaxLength).ToLowerInvariant();

        if (!EmailPattern().IsMatch(workEmail))
        {
            throw new DomainException("Work email is not a valid email address.");
        }

        if (hireDate == default)
        {
            throw new DomainException("Hire date is required.");
        }

        if (!Enum.IsDefined(employmentType))
        {
            throw new DomainException("Employment type is not a recognised value.");
        }

        return new Employee(employeeNumber, firstName, lastName, workEmail, hireDate, employmentType);
    }

    public void AssignManager(Guid? managerId)
    {
        if (managerId == Id)
        {
            throw new DomainException("An employee cannot manage themselves.");
        }

        ManagerId = managerId;
    }

    public void LinkToUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        UserId = userId;
    }

    public void ChangeWorkEmail(string workEmail)
    {
        workEmail = Require(workEmail, "Work email", EmailMaxLength).ToLowerInvariant();

        if (!EmailPattern().IsMatch(workEmail))
        {
            throw new DomainException("Work email is not a valid email address.");
        }

        WorkEmail = workEmail;
    }

    private static string Require(string? value, string label, int maxLength)
    {
        value = value?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            throw new DomainException($"{label} is required.");
        }

        if (value.Length > maxLength)
        {
            throw new DomainException($"{label} must not exceed {maxLength} characters.");
        }

        return value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
