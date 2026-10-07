using HRMS.Application.Authentication.Dtos;

namespace HRMS.Application.Authentication;

public interface IAuthenticationService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<AuthResponse> CompleteTwoFactorAsync(TwoFactorLoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default);
}
