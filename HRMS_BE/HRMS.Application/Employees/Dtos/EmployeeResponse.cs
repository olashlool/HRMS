namespace HRMS.Application.Employees.Dtos;

public sealed record EmployeeResponse(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string WorkEmail,
    DateOnly HireDate,
    string EmploymentType,
    Guid? UserId,
    DateTimeOffset CreatedAtUtc);
