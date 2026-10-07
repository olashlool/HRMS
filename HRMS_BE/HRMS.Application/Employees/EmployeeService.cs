using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Application.Employees.Dtos;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Employees;

public sealed class EmployeeService : IEmployeeService
{
    private readonly ITenantDbContext _tenantDb;
    private readonly ICurrentTenant _currentTenant;
    private readonly IEntitlementResolver _entitlements;

    public EmployeeService(
        ITenantDbContext tenantDb,
        ICurrentTenant currentTenant,
        IEntitlementResolver entitlements)
    {
        _tenantDb = tenantDb;
        _currentTenant = currentTenant;
        _entitlements = entitlements;
    }

    public async Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        await GuardSeatLimitAsync(cancellationToken);

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
                e.ManagerId,
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

    public async Task<EmployeeResponse> LinkUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var employee = await RequireAsync(id, cancellationToken);

        employee.LinkToUser(userId);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        return ToResponse(employee);
    }

    public async Task<EmployeeResponse> AssignManagerAsync(Guid id, Guid? managerId, CancellationToken cancellationToken = default)
    {
        var employee = await RequireAsync(id, cancellationToken);

        if (managerId is { } manager && !await _tenantDb.Employees.AnyAsync(e => e.Id == manager, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), manager);
        }

        employee.AssignManager(managerId);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        return ToResponse(employee);
    }

    private async Task<Employee> RequireAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _tenantDb.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return employee ?? throw new NotFoundException(nameof(Employee), id);
    }

    private async Task GuardSeatLimitAsync(CancellationToken cancellationToken)
    {
        var entitlements = await _entitlements.ResolveAsync(_currentTenant.TenantId, cancellationToken);

        if (entitlements.MaxEmployees is not { } limit)
        {
            return;
        }

        var used = await _tenantDb.Employees.CountAsync(cancellationToken);

        if (used >= limit)
        {
            throw new PlanLimitExceededException(
                $"The {entitlements.PlanName} plan allows {limit} employees and {used} are already in use.");
        }
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
            employee.ManagerId,
            employee.CreatedAtUtc);
}
