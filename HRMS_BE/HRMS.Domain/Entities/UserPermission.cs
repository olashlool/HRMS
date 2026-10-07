using HRMS.Domain.Authorization;
using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class UserPermission : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Permission { get; private set; } = null!;
    public bool IsGranted { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }

    private UserPermission()
    {
    }

    private UserPermission(Guid userId, string permission, bool isGranted, DateTimeOffset assignedAtUtc)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        Permission = permission;
        IsGranted = isGranted;
        AssignedAtUtc = assignedAtUtc;
    }

    public static UserPermission Allow(Guid userId, string permission, DateTimeOffset now) =>
        Create(userId, permission, true, now);

    public static UserPermission Deny(Guid userId, string permission, DateTimeOffset now) =>
        Create(userId, permission, false, now);

    private static UserPermission Create(Guid userId, string permission, bool isGranted, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        permission = permission?.Trim() ?? string.Empty;

        if (!Permissions.IsKnown(permission))
        {
            throw new DomainException($"'{permission}' is not a recognised permission.");
        }

        return new UserPermission(userId, permission, isGranted, now);
    }
}
