using HRMS.Application.Authentication.Dtos;

namespace HRMS.Application.Authentication;

public interface IAccountService
{
    Task SendEmailConfirmationAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<TwoFactorSetupResponse> StartTwoFactorSetupAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RecoveryCodesResponse> EnableTwoFactorAsync(Guid userId, EnableTwoFactorRequest request, CancellationToken cancellationToken = default);

    Task DisableTwoFactorAsync(Guid userId, EnableTwoFactorRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoginAttemptResponse>> GetLoginHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
}
