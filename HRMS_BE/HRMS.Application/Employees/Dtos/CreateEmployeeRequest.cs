using HRMS.Domain.Entities.Enums;

namespace HRMS.Application.Employees.Dtos;

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string WorkEmail,
    DateOnly HireDate,
    EmploymentType EmploymentType);

public sealed record LinkUserRequest(Guid UserId);

public sealed record AssignManagerRequest(Guid? ManagerId);
