namespace HRMS.Application.Authentication.Dtos;

public sealed record ConfirmEmailRequest(string UserId, string Token);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string UserId, string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record TwoFactorSetupResponse(string Secret, string AuthenticatorUri);

public sealed record EnableTwoFactorRequest(string Code);

public sealed record TwoFactorChallengeResponse(string ChallengeToken, DateTimeOffset ExpiresAtUtc);

public sealed record TwoFactorLoginRequest(string ChallengeToken, string Code);

public sealed record RecoveryCodesResponse(IReadOnlyList<string> Codes);

public sealed record LoginAttemptResponse(
    DateTimeOffset OccurredAtUtc,
    bool Succeeded,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent);
