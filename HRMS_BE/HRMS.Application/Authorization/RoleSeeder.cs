using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Authorization;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Authorization;

public sealed class RoleSeeder : IRoleSeeder
{
    private readonly ICatalogDbContext _catalog;

    public RoleSeeder(ICatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    /// <summary>
    /// Creates the built-in roles for a tenant and reconciles their permissions.
    /// Reconciling matters: when a release adds a permission to a built-in role,
    /// a create-only seeder would leave every existing tenant behind.
    /// Custom roles are never touched.
    /// </summary>
    public async Task<Guid> EnsureTenantRolesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _catalog.Roles
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var adminRoleId = Guid.Empty;

        foreach (var (name, permissions) in SystemRoles.Defaults)
        {
            var normalized = name.ToUpperInvariant();
            var role = existing.FirstOrDefault(r => r.NormalizedName == normalized);

            if (role is null)
            {
                role = Role.CreateForTenant(tenantId, name, $"Built-in {name} role.", isSystemRole: true);
                _catalog.Roles.Add(role);
            }

            if (role.IsSystemRole)
            {
                await ReconcilePermissionsAsync(role.Id, permissions, cancellationToken);
            }

            if (name == SystemRoles.TenantAdmin)
            {
                adminRoleId = role.Id;
            }
        }

        await _catalog.SaveChangesAsync(cancellationToken);

        return adminRoleId;
    }

    private async Task ReconcilePermissionsAsync(
        Guid roleId,
        IReadOnlySet<string> expected,
        CancellationToken cancellationToken)
    {
        var current = await _catalog.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        foreach (var missing in expected.Where(p => current.All(c => c.Permission != p)))
        {
            _catalog.RolePermissions.Add(RolePermission.Grant(roleId, missing));
        }

        var withdrawn = current.Where(c => !expected.Contains(c.Permission)).ToList();

        _catalog.RolePermissions.RemoveRange(withdrawn);
    }
}
