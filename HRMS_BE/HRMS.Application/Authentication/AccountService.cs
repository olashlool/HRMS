using HRMS.Application.Authentication.Dtos;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Common;
using HRMS.Domain.Entities;
using HRMS.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Application.Authentication;

public sealed class AccountService : IAccountService
{
    public const string TwoFactorIssuer = "HRMS";

    private const int RecoveryCodeCount = 10;
    private const int LoginHistoryPageSize = 50;

    private static readonly TimeSpan EmailConfirmationLifetime = TimeSpan.FromDays(2);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);

    private readonly ICatalogDbContext _catalog;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenGenerator _tokenGenerator;
    private readonly ITotpGenerator _totp;
    private readonly ISecretProtector _secretProtector;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        ICatalogDbContext catalog,
        IPasswordHasher passwordHasher,
        IRefreshTokenGenerator tokenGenerator,
        ITotpGenerator totp,
        ISecretProtector secretProtector,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<AccountService> logger)
    {
        _catalog = catalog;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _totp = totp;
        _secretProtector = secretProtector;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SendEmailConfirmationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        if (user.EmailConfirmed)
        {
            return;
        }

        var token = await IssueTokenAsync(
            user.Id, UserTokenPurpose.EmailConfirmation, EmailConfirmationLifetime, cancellationToken);

        await _emailSender.SendAsync(
            user.Email,
            "Confirm your email address",
            $"userId={user.Id}&token={token}",
            cancellationToken);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new InvalidTokenException();
        }

        var token = await ConsumeTokenAsync(
            userId, UserTokenPurpose.EmailConfirmation, request.Token, now, cancellationToken);

        var user = await RequireUserAsync(token.UserId, cancellationToken);

        user.ConfirmEmail();

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (request.Email?.Trim() ?? string.Empty).ToUpperInvariant();

        var user = await _catalog.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Password reset requested for an address with no account.");
            return;
        }

        var token = await IssueTokenAsync(
            user.Id, UserTokenPurpose.PasswordReset, PasswordResetLifetime, cancellationToken);

        await _emailSender.SendAsync(
            user.Email,
            "Reset your password",
            $"userId={user.Id}&token={token}",
            cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new InvalidTokenException();
        }

        var token = await ConsumeTokenAsync(
            userId, UserTokenPurpose.PasswordReset, request.Token, now, cancellationToken);

        var user = await RequireUserAsync(token.UserId, cancellationToken);

        user.SetPasswordHash(_passwordHasher.Hash(RequirePassword(request.NewPassword)));
        user.RegisterSuccessfulLogin(now);

        await RevokeAllSessionsAsync(user.Id, now, "Password was reset.", cancellationToken);

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var user = await RequireUserAsync(userId, cancellationToken);

        if (_passwordHasher.Verify(user.PasswordHash, request.CurrentPassword ?? string.Empty)
            == PasswordVerificationOutcome.Failed)
        {
            throw new InvalidCredentialsException();
        }

        user.SetPasswordHash(_passwordHasher.Hash(RequirePassword(request.NewPassword)));

        await RevokeAllSessionsAsync(user.Id, now, "Password was changed.", cancellationToken);

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    public async Task<TwoFactorSetupResponse> StartTwoFactorSetupAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        var secret = _totp.CreateSecret();

        user.SetPendingTwoFactorSecret(_secretProtector.Protect(secret));

        await _catalog.SaveChangesAsync(cancellationToken);

        var uri = _totp.BuildAuthenticatorUri(TwoFactorIssuer, user.Email, secret);

        return new TwoFactorSetupResponse(secret, uri);
    }

    public async Task<RecoveryCodesResponse> EnableTwoFactorAsync(
        Guid userId,
        EnableTwoFactorRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var user = await RequireUserAsync(userId, cancellationToken);

        if (user.TwoFactorSecret is null)
        {
            throw new InvalidTokenException();
        }

        var secret = _secretProtector.Unprotect(user.TwoFactorSecret);

        if (!_totp.Verify(secret, request.Code ?? string.Empty, now))
        {
            throw new InvalidTokenException();
        }

        user.EnableTwoFactor();

        var existing = await _catalog.RecoveryCodes
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);

        _catalog.RecoveryCodes.RemoveRange(existing);

        var codes = new List<string>(RecoveryCodeCount);

        for (var i = 0; i < RecoveryCodeCount; i++)
        {
            var code = _tokenGenerator.Generate()[..12].ToUpperInvariant();
            codes.Add(code);

            _catalog.RecoveryCodes.Add(RecoveryCode.Issue(user.Id, _tokenGenerator.Hash(code), now));
        }

        await _catalog.SaveChangesAsync(cancellationToken);

        return new RecoveryCodesResponse(codes);
    }

    public async Task DisableTwoFactorAsync(
        Guid userId,
        EnableTwoFactorRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var user = await RequireUserAsync(userId, cancellationToken);

        if (!user.TwoFactorEnabled || user.TwoFactorSecret is null)
        {
            return;
        }

        var secret = _secretProtector.Unprotect(user.TwoFactorSecret);

        if (!_totp.Verify(secret, request.Code ?? string.Empty, now))
        {
            throw new InvalidTokenException();
        }

        user.DisableTwoFactor();

        var codes = await _catalog.RecoveryCodes
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);

        _catalog.RecoveryCodes.RemoveRange(codes);

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LoginAttemptResponse>> GetLoginHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _catalog.LoginAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(LoginHistoryPageSize)
            .Select(a => new LoginAttemptResponse(
                a.OccurredAtUtc,
                a.Succeeded,
                a.FailureReason,
                a.IpAddress,
                a.UserAgent))
            .ToListAsync(cancellationToken);
    }

    private async Task<string> IssueTokenAsync(
        Guid userId,
        UserTokenPurpose purpose,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var outstanding = await _catalog.UserTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in outstanding)
        {
            token.Consume(now);
        }

        var value = _tokenGenerator.Generate();

        _catalog.UserTokens.Add(
            UserToken.Issue(userId, purpose, _tokenGenerator.Hash(value), now, lifetime));

        await _catalog.SaveChangesAsync(cancellationToken);

        return value;
    }

    private async Task<UserToken> ConsumeTokenAsync(
        Guid userId,
        UserTokenPurpose purpose,
        string? value,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var hash = _tokenGenerator.Hash(value ?? string.Empty);

        var token = await _catalog.UserTokens
            .FirstOrDefaultAsync(
                t => t.UserId == userId && t.Purpose == purpose && t.TokenHash == hash,
                cancellationToken);

        if (token is null || !token.IsUsable(now))
        {
            throw new InvalidTokenException();
        }

        token.Consume(now);

        return token;
    }

    private async Task RevokeAllSessionsAsync(
        Guid userId,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
    {
        var tokens = await _catalog.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(now, reason);
        }
    }

    private async Task<User> RequireUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _catalog.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user ?? throw new NotFoundException(nameof(User), userId);
    }

    private static string RequirePassword(string? password)
    {
        password ??= string.Empty;

        if (password.Length < 12)
        {
            throw new DomainException("Password must be at least 12 characters long.");
        }

        if (password.Length > 256)
        {
            throw new DomainException("Password must not exceed 256 characters.");
        }

        return password;
    }
}
