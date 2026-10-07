using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class RecoveryCode : BaseEntity
{
    public const int CodeHashLength = 64;

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }

    private RecoveryCode()
    {
    }

    private RecoveryCode(Guid userId, string codeHash, DateTimeOffset createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
    }

    public static RecoveryCode Issue(Guid userId, string codeHash, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new DomainException("Code hash is required.");
        }

        return new RecoveryCode(userId, codeHash, now);
    }

    public bool IsUsable => UsedAtUtc is null;

    public void Use(DateTimeOffset now)
    {
        if (UsedAtUtc is not null)
        {
            throw new DomainException("This recovery code has already been used.");
        }

        UsedAtUtc = now;
    }
}
