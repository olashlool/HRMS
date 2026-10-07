using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Employees.Dtos;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly ITenantDbContext _tenantDb;

    public EmployeeService(ITenantDbContext tenantDb)
    {
        _tenantDb = tenantDb;
    }

    public async Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = Employee.Create(
            request.EmployeeNumber,
            request.FirstName,
            request.LastName,
            request.WorkEmail,
            request.HireDate,
            request.EmploymentType);

        _tenantDb.Employees.Add(employee);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        return ToResponse(employee);
    }

    public async Task<EmployeeResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employee = await _tenantDb.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        return ToResponse(employee);
    }

    public async Task<IReadOnlyList<EmployeeResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _tenantDb.Employees
            .AsNoTracking()
            .OrderBy(e => e.EmployeeNumber)
            .Select(e => new EmployeeResponse(
                e.Id,
                e.EmployeeNumber,
                e.FirstName,
                e.LastName,
                e.WorkEmail,
                e.HireDate,
                e.EmploymentType.ToString(),
                e.UserId,
                e.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _tenantDb.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(nameof(Employee), id);
        }

        _tenantDb.Employees.Remove(employee);
        await _tenantDb.SaveChangesAsync(cancellationToken);
    }

    private static EmployeeResponse ToResponse(Employee employee) =>
        new(employee.Id,
            employee.EmployeeNumber,
            employee.FirstName,
            employee.LastName,
            employee.WorkEmail,
            employee.HireDate,
            employee.EmploymentType.ToString(),
            employee.UserId,
            employee.CreatedAtUtc);
}
