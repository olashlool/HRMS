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

    public async Task<Guid> EnsureTenantRolesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _catalog.Roles
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        Guid adminRoleId = Guid.Empty;

        foreach (var (name, permissions) in SystemRoles.Defaults)
        {
            var normalized = name.ToUpperInvariant();
            var role = existing.FirstOrDefault(r => r.NormalizedName == normalized);

            if (role is null)
            {
                role = Role.CreateForTenant(tenantId, name, $"Built-in {name} role.", isSystemRole: true);
                _catalog.Roles.Add(role);

                foreach (var permission in permissions)
                {
                    _catalog.RolePermissions.Add(RolePermission.Grant(role.Id, permission));
                }
            }

            if (name == SystemRoles.TenantAdmin)
            {
                adminRoleId = role.Id;
            }
        }

        await _catalog.SaveChangesAsync(cancellationToken);

        return adminRoleId;
    }
}
