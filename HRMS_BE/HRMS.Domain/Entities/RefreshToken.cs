using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public const int TokenHashLength = 64;
    public const int RevokedReasonMaxLength = 200;

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public Guid FamilyId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedReason { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }

    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? createdByIp)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset now,
        TimeSpan lifetime,
        string? createdByIp)
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
            throw new DomainException("Refresh token lifetime must be positive.");
        }

        return new RefreshToken(userId, tokenHash, familyId, now, now.Add(lifetime), createdByIp);
    }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;

    public void Revoke(DateTimeOffset now, string reason, Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = now;
        RevokedReason = reason.Length > RevokedReasonMaxLength
            ? reason[..RevokedReasonMaxLength]
            : reason;
        ReplacedByTokenId = replacedByTokenId;
    }
}
