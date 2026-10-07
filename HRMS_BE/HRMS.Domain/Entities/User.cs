using System.Text.RegularExpressions;
using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed partial class User : AuditableEntity
{
    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 200;
    public const int MaxFailedAccessAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public bool EmailConfirmed { get; private set; }
    public string SecurityStamp { get; private set; } = null!;
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }

    private User()
    {
    }

    private User(Guid tenantId, string email, string fullName, string passwordHash)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        FullName = fullName;
        PasswordHash = passwordHash;
        SecurityStamp = NewSecurityStamp();
    }

    public static User Create(Guid tenantId, string email, string fullName, string passwordHash)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        email = email?.Trim().ToLowerInvariant() ?? string.Empty;
        fullName = fullName?.Trim() ?? string.Empty;

        if (email.Length == 0)
        {
            throw new DomainException("Email is required.");
        }

        if (email.Length > EmailMaxLength)
        {
            throw new DomainException($"Email must not exceed {EmailMaxLength} characters.");
        }

        if (!EmailPattern().IsMatch(email))
        {
            throw new DomainException("Email is not a valid email address.");
        }

        if (fullName.Length == 0)
        {
            throw new DomainException("Full name is required.");
        }

        if (fullName.Length > FullNameMaxLength)
        {
            throw new DomainException($"Full name must not exceed {FullNameMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        return new User(tenantId, email, fullName, passwordHash);
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndUtc is not null && LockoutEndUtc > now;

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        AccessFailedCount++;

        if (AccessFailedCount >= MaxFailedAccessAttempts)
        {
            LockoutEndUtc = now.Add(LockoutDuration);
            AccessFailedCount = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = now;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
        RotateSecurityStamp();
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    public void SetPendingTwoFactorSecret(string protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(protectedSecret))
        {
            throw new DomainException("A two-factor secret is required.");
        }

        if (TwoFactorEnabled)
        {
            throw new DomainException("Two-factor authentication is already enabled.");
        }

        TwoFactorSecret = protectedSecret;
    }

    public void EnableTwoFactor()
    {
        if (string.IsNullOrWhiteSpace(TwoFactorSecret))
        {
            throw new DomainException("No pending two-factor secret has been set up.");
        }

        TwoFactorEnabled = true;
        RotateSecurityStamp();
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        TwoFactorEnabled = false;
        RotateSecurityStamp();
    }

    public void RotateSecurityStamp()
    {
        SecurityStamp = NewSecurityStamp();
    }

    private static string NewSecurityStamp() => Guid.CreateVersion7().ToString("N");

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
