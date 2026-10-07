namespace HRMS.Application.Authentication.Dtos;

public sealed record RegisterRequest(string TenantSlug, string Email, string Password, string FullName);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    Guid TenantId,
    string TenantSlug,
    bool EmailConfirmed);
