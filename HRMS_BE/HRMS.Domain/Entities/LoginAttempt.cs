using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class LoginAttempt : BaseEntity
{
    public const int EmailMaxLength = 256;
    public const int FailureReasonMaxLength = 100;
    public const int IpAddressMaxLength = 45;
    public const int UserAgentMaxLength = 512;

    public Guid? UserId { get; private set; }
    public string AttemptedEmail { get; private set; } = null!;
    public bool Succeeded { get; private set; }
    public string? FailureReason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    private LoginAttempt()
    {
    }

    private LoginAttempt(
        Guid? userId,
        string attemptedEmail,
        bool succeeded,
        string? failureReason,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAtUtc)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        AttemptedEmail = attemptedEmail;
        Succeeded = succeeded;
        FailureReason = failureReason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredAtUtc = occurredAtUtc;
    }

    public static LoginAttempt Success(
        Guid userId,
        string attemptedEmail,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now) =>
        new(userId, Normalise(attemptedEmail, EmailMaxLength)!, true, null,
            Normalise(ipAddress, IpAddressMaxLength), Normalise(userAgent, UserAgentMaxLength), now);

    public static LoginAttempt Failure(
        Guid? userId,
        string attemptedEmail,
        string failureReason,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now) =>
        new(userId, Normalise(attemptedEmail, EmailMaxLength)!, false,
            Normalise(failureReason, FailureReasonMaxLength),
            Normalise(ipAddress, IpAddressMaxLength), Normalise(userAgent, UserAgentMaxLength), now);

    private static string? Normalise(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        value = value.Trim();

        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
