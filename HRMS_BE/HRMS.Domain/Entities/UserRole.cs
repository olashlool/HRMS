using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class UserRole : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }

    private UserRole()
    {
    }

    private UserRole(Guid userId, Guid roleId, DateTimeOffset assignedAtUtc)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc;
    }

    public static UserRole Assign(Guid userId, Guid roleId, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (roleId == Guid.Empty)
        {
            throw new DomainException("Role id is required.");
        }

        return new UserRole(userId, roleId, now);
    }
}
