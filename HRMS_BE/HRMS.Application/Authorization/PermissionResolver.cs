using HRMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Authorization;

public sealed class PermissionResolver : IPermissionResolver
{
    private readonly ICatalogDbContext _catalog;

    public PermissionResolver(ICatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    public async Task<IReadOnlySet<string>> ResolveAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var fromRoles = await _catalog.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(
                _catalog.RolePermissions.AsNoTracking(),
                ur => ur.RoleId,
                rp => rp.RoleId,
                (_, rp) => rp.Permission)
            .Distinct()
            .ToListAsync(cancellationToken);

        var overrides = await _catalog.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => new { up.Permission, up.IsGranted })
            .ToListAsync(cancellationToken);

        var effective = new HashSet<string>(fromRoles, StringComparer.Ordinal);

        foreach (var grant in overrides.Where(o => o.IsGranted))
        {
            effective.Add(grant.Permission);
        }

        foreach (var deny in overrides.Where(o => !o.IsGranted))
        {
            effective.Remove(deny.Permission);
        }

        return effective;
    }
}
