using HRMS.Domain.Common;
using HRMS.Domain.Entities.Enums;

namespace HRMS.Domain.Entities;

public sealed class UserToken : BaseEntity
{
    public const int TokenHashLength = 64;

    public Guid UserId { get; private set; }
    public UserTokenPurpose Purpose { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    private UserToken()
    {
    }

    private UserToken(
        Guid userId,
        UserTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static UserToken Issue(
        Guid userId,
        UserTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Token hash is required.");
        }

        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException("Token lifetime must be positive.");
        }

        return new UserToken(userId, purpose, tokenHash, now, now.Add(lifetime));
    }

    public bool IsUsable(DateTimeOffset now) => ConsumedAtUtc is null && ExpiresAtUtc > now;

    public void Consume(DateTimeOffset now)
    {
        if (ConsumedAtUtc is not null)
        {
            throw new DomainException("This token has already been used.");
        }

        ConsumedAtUtc = now;
    }
}
