using HRMS.Application.Authorization.Dtos;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Authorization;
using HRMS.Domain.Common;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Authorization;

public sealed class RoleAdministrationService : IRoleAdministrationService
{
    private readonly ICatalogDbContext _catalog;
    private readonly ICurrentTenant _currentTenant;
    private readonly IPermissionResolver _permissionResolver;
    private readonly TimeProvider _timeProvider;

    public RoleAdministrationService(
        ICatalogDbContext catalog,
        ICurrentTenant currentTenant,
        IPermissionResolver permissionResolver,
        TimeProvider timeProvider)
    {
        _catalog = catalog;
        _currentTenant = currentTenant;
        _permissionResolver = permissionResolver;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<RoleResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;

        var roles = await _catalog.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToList();

        var permissions = await _catalog.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .ToListAsync(cancellationToken);

        return roles
            .Select(r => new RoleResponse(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                permissions.Where(p => p.RoleId == r.Id).Select(p => p.Permission).Order().ToList()))
            .ToList();
    }

    public async Task<RoleResponse> CreateAsync(
        Guid actorUserId,
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;

        await GuardAgainstEscalationAsync(actorUserId, request.Permissions, cancellationToken);

        var role = Role.CreateForTenant(tenantId, request.Name, request.Description);

        _catalog.Roles.Add(role);

        foreach (var permission in Distinct(request.Permissions))
        {
            GuardTenantScoped(permission);
            _catalog.RolePermissions.Add(RolePermission.Grant(role.Id, permission));
        }

        await _catalog.SaveChangesAsync(cancellationToken);

        return new RoleResponse(
            role.Id, role.Name, role.Description, role.IsSystemRole, Distinct(request.Permissions).Order().ToList());
    }

    public async Task<RoleResponse> SetPermissionsAsync(
        Guid actorUserId,
        Guid roleId,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await RequireTenantRoleAsync(roleId, cancellationToken);

        if (role.IsSystemRole)
        {
            throw new ForbiddenException("The permissions of a built-in role cannot be changed.");
        }

        await GuardAgainstEscalationAsync(actorUserId, request.Permissions, cancellationToken);

        var existing = await _catalog.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        _catalog.RolePermissions.RemoveRange(existing);

        var permissions = Distinct(request.Permissions);

        foreach (var permission in permissions)
        {
            GuardTenantScoped(permission);
            _catalog.RolePermissions.Add(RolePermission.Grant(role.Id, permission));
        }

        await _catalog.SaveChangesAsync(cancellationToken);

        return new RoleResponse(role.Id, role.Name, role.Description, role.IsSystemRole, permissions.Order().ToList());
    }

    public async Task AssignRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await RequireTenantRoleAsync(roleId, cancellationToken);
        var target = await RequireTenantUserAsync(targetUserId, cancellationToken);

        var rolePermissions = await _catalog.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        await GuardAgainstEscalationAsync(actorUserId, rolePermissions, cancellationToken);

        var alreadyAssigned = await _catalog.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == target.Id && ur.RoleId == role.Id, cancellationToken);

        if (alreadyAssigned)
        {
            return;
        }

        _catalog.UserRoles.Add(UserRole.Assign(target.Id, role.Id, _timeProvider.GetUtcNow()));

        target.RotateSecurityStamp();

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    public async Task SetUserPermissionOverrideAsync(
        Guid actorUserId,
        Guid targetUserId,
        SetUserPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var target = await RequireTenantUserAsync(targetUserId, cancellationToken);
        var permission = request.Permission?.Trim() ?? string.Empty;

        GuardTenantScoped(permission);

        if (request.IsGranted)
        {
            await GuardAgainstEscalationAsync(actorUserId, [permission], cancellationToken);
        }

        var existing = await _catalog.UserPermissions
            .Where(up => up.UserId == target.Id && up.Permission == permission)
            .ToListAsync(cancellationToken);

        _catalog.UserPermissions.RemoveRange(existing);

        var now = _timeProvider.GetUtcNow();

        _catalog.UserPermissions.Add(request.IsGranted
            ? UserPermission.Allow(target.Id, permission, now)
            : UserPermission.Deny(target.Id, permission, now));

        target.RotateSecurityStamp();

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    public async Task<EffectivePermissionsResponse> GetEffectivePermissionsAsync(
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var target = await RequireTenantUserAsync(targetUserId, cancellationToken);
        var permissions = await _permissionResolver.ResolveAsync(target.Id, cancellationToken);

        return new EffectivePermissionsResponse(target.Id, permissions.Order().ToList());
    }

    private async Task GuardAgainstEscalationAsync(
        Guid actorUserId,
        IEnumerable<string> requestedPermissions,
        CancellationToken cancellationToken)
    {
        var actorPermissions = await _permissionResolver.ResolveAsync(actorUserId, cancellationToken);

        var exceeded = Distinct(requestedPermissions)
            .Where(p => !actorPermissions.Contains(p))
            .Order()
            .ToList();

        if (exceeded.Count > 0)
        {
            throw new ForbiddenException(
                $"You cannot grant permissions you do not hold yourself: {string.Join(", ", exceeded)}.");
        }
    }

    private static void GuardTenantScoped(string permission)
    {
        if (!Permissions.TenantScoped.Contains(permission))
        {
            throw new DomainException($"'{permission}' cannot be granted inside a tenant.");
        }
    }

    private async Task<Role> RequireTenantRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId;

        var role = await _catalog.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId, cancellationToken);

        return role ?? throw new NotFoundException(nameof(Role), roleId);
    }

    private async Task<User> RequireTenantUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId;

        var user = await _catalog.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

        return user ?? throw new NotFoundException(nameof(User), userId);
    }

    private static List<string> Distinct(IEnumerable<string>? permissions) =>
        (permissions ?? [])
        .Select(p => p?.Trim() ?? string.Empty)
        .Where(p => p.Length > 0)
        .Distinct(StringComparer.Ordinal)
        .ToList();
}
