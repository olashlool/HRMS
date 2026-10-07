using HRMS.Domain.Authorization;
using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class RolePermission : BaseEntity
{
    public const int PermissionMaxLength = 100;

    public Guid RoleId { get; private set; }
    public string Permission { get; private set; } = null!;

    private RolePermission()
    {
    }

    private RolePermission(Guid roleId, string permission)
    {
        Id = Guid.CreateVersion7();
        RoleId = roleId;
        Permission = permission;
    }

    public static RolePermission Grant(Guid roleId, string permission)
    {
        if (roleId == Guid.Empty)
        {
            throw new DomainException("Role id is required.");
        }

        permission = permission?.Trim() ?? string.Empty;

        if (!Permissions.IsKnown(permission))
        {
            throw new DomainException($"'{permission}' is not a recognised permission.");
        }

        return new RolePermission(roleId, permission);
    }
}
