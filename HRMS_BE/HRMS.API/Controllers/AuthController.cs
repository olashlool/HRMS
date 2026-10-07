using HRMS.API.Filters;
using HRMS.Application.Authentication;
using HRMS.Application.Authentication.Dtos;
using HRMS.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAccountService _accountService;

    public AuthController(IAuthenticationService authenticationService, IAccountService accountService)
    {
        _authenticationService = authenticationService;
        _accountService = accountService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [DevelopmentOnly]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        return await _authenticationService.RegisterAsync(request, ClientIp(), cancellationToken);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResult>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        return await _authenticationService.LoginAsync(request, ClientIp(), UserAgent(), cancellationToken);
    }

    [HttpPost("two-factor")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> CompleteTwoFactor(
        TwoFactorLoginRequest request,
        CancellationToken cancellationToken)
    {
        return await _authenticationService.CompleteTwoFactorAsync(request, ClientIp(), UserAgent(), cancellationToken);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        return await _authenticationService.RefreshAsync(request, ClientIp(), cancellationToken);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await _authenticationService.LogoutAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        await _accountService.ConfirmEmailAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _accountService.ForgotPasswordAsync(request, cancellationToken);

        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _accountService.ResetPasswordAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _accountService.ChangePasswordAsync(CurrentUserId(), request, cancellationToken);

        return NoContent();
    }

    [HttpPost("send-email-confirmation")]
    [Authorize]
    public async Task<IActionResult> SendEmailConfirmation(CancellationToken cancellationToken)
    {
        await _accountService.SendEmailConfirmationAsync(CurrentUserId(), cancellationToken);

        return Accepted();
    }

    [HttpPost("two-factor/setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupResponse>> StartTwoFactorSetup(CancellationToken cancellationToken)
    {
        return await _accountService.StartTwoFactorSetupAsync(CurrentUserId(), cancellationToken);
    }

    [HttpPost("two-factor/enable")]
    [Authorize]
    public async Task<ActionResult<RecoveryCodesResponse>> EnableTwoFactor(
        EnableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        return await _accountService.EnableTwoFactorAsync(CurrentUserId(), request, cancellationToken);
    }

    [HttpPost("two-factor/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(
        EnableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        await _accountService.DisableTwoFactorAsync(CurrentUserId(), request, cancellationToken);

        return NoContent();
    }

    [HttpGet("login-history")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<LoginAttemptResponse>>> LoginHistory(
        CancellationToken cancellationToken)
    {
        return Ok(await _accountService.GetLoginHistoryAsync(CurrentUserId(), cancellationToken));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserResponse> Me()
    {
        return new CurrentUserResponse(
            CurrentUserId(),
            User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
            User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? string.Empty,
            Guid.Parse(User.FindFirst(AccessTokenGenerator.TenantIdClaim)?.Value ?? Guid.Empty.ToString()),
            User.FindFirst(AccessTokenGenerator.TenantSlugClaim)?.Value ?? string.Empty,
            EmailConfirmed: false);
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? Guid.Empty.ToString());

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
