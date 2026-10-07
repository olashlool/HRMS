using HRMS.Application.Employees.Dtos;

namespace HRMS.Application.Employees;

public interface IEmployeeService
{
    Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EmployeeResponse> LinkUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<EmployeeResponse> AssignManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default);
}
