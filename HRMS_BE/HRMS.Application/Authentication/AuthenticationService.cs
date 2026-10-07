using HRMS.Application.Authentication.Dtos;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Common;
using HRMS.Domain.Entities;
using HRMS.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Application.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private const int MinimumPasswordLength = 12;
    private const int MaximumPasswordLength = 256;

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly ICatalogDbContext _catalog;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITotpGenerator _totp;
    private readonly ISecretProtector _secretProtector;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ICatalogDbContext catalog,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITotpGenerator totp,
        ISecretProtector secretProtector,
        TimeProvider timeProvider,
        ILogger<AuthenticationService> logger)
    {
        _catalog = catalog;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _totp = totp;
        _secretProtector = secretProtector;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var slug = request.TenantSlug?.Trim().ToLowerInvariant() ?? string.Empty;

        var tenant = await _catalog.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug && t.Status == TenantStatus.Active, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), slug);
        }

        var normalizedEmail = (request.Email?.Trim() ?? string.Empty).ToUpperInvariant();

        var emailTaken = await _catalog.Users
            .AsNoTracking()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException("That email address is already registered.");
        }

        var passwordHash = _passwordHasher.Hash(RequirePassword(request.Password));

        var user = User.Create(
            tenant.Id,
            request.Email ?? string.Empty,
            request.FullName ?? string.Empty,
            passwordHash);

        _catalog.Users.Add(user);
        await _catalog.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, tenant, Guid.CreateVersion7(), ipAddress, cancellationToken);
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var attemptedEmail = request.Email?.Trim() ?? string.Empty;
        var normalizedEmail = attemptedEmail.ToUpperInvariant();

        var user = await _catalog.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _passwordHasher.Hash(request.Password ?? string.Empty);
            await RecordAttemptAsync(null, attemptedEmail, "Unknown account.", ipAddress, userAgent, now, cancellationToken);

            throw new InvalidCredentialsException();
        }

        if (user.IsLockedOut(now))
        {
            _logger.LogWarning("Login attempt for locked out user {UserId}.", user.Id);
            await RecordAttemptAsync(user.Id, attemptedEmail, "Account locked out.", ipAddress, userAgent, now, cancellationToken);

            throw new InvalidCredentialsException();
        }

        var outcome = _passwordHasher.Verify(user.PasswordHash, request.Password ?? string.Empty);

        if (outcome == PasswordVerificationOutcome.Failed)
        {
            user.RegisterFailedLogin(now);
            await RecordAttemptAsync(user.Id, attemptedEmail, "Incorrect password.", ipAddress, userAgent, now, cancellationToken);

            throw new InvalidCredentialsException();
        }

        if (outcome == PasswordVerificationOutcome.SuccessButNeedsRehash)
        {
            user.SetPasswordHash(_passwordHasher.Hash(request.Password!));
        }

        var tenant = await _catalog.Tenants
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        if (tenant is null || tenant.Status != TenantStatus.Active)
        {
            await RecordAttemptAsync(user.Id, attemptedEmail, "Tenant is not active.", ipAddress, userAgent, now, cancellationToken);

            throw new InvalidCredentialsException();
        }

        if (user.TwoFactorEnabled)
        {
            var challenge = await IssueTwoFactorChallengeAsync(user, now, cancellationToken);

            return LoginResult.TwoFactorRequired(challenge);
        }

        user.RegisterSuccessfulLogin(now);
        await RecordAttemptAsync(user.Id, attemptedEmail, null, ipAddress, userAgent, now, cancellationToken);

        var tokens = await IssueTokensAsync(user, tenant, Guid.CreateVersion7(), ipAddress, cancellationToken);

        return LoginResult.Authenticated(tokens);
    }

    public async Task<AuthResponse> CompleteTwoFactorAsync(
        TwoFactorLoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var hash = _refreshTokenGenerator.Hash(request.ChallengeToken ?? string.Empty);

        var challenge = await _catalog.UserTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash && t.Purpose == UserTokenPurpose.TwoFactorChallenge,
                cancellationToken);

        if (challenge is null || !challenge.IsUsable(now))
        {
            throw new InvalidCredentialsException();
        }

        var user = await _catalog.Users.FirstOrDefaultAsync(u => u.Id == challenge.UserId, cancellationToken);

        if (user is null || !user.TwoFactorEnabled || user.TwoFactorSecret is null || user.IsLockedOut(now))
        {
            throw new InvalidCredentialsException();
        }

        var code = request.Code?.Trim() ?? string.Empty;
        var accepted = _totp.Verify(_secretProtector.Unprotect(user.TwoFactorSecret), code, now);

        if (!accepted)
        {
            accepted = await TryConsumeRecoveryCodeAsync(user.Id, code, now, cancellationToken);
        }

        if (!accepted)
        {
            user.RegisterFailedLogin(now);
            await RecordAttemptAsync(user.Id, user.Email, "Incorrect second factor.", ipAddress, userAgent, now, cancellationToken);

            throw new InvalidCredentialsException();
        }

        challenge.Consume(now);

        var tenant = await _catalog.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        if (tenant is null || tenant.Status != TenantStatus.Active)
        {
            throw new InvalidCredentialsException();
        }

        user.RegisterSuccessfulLogin(now);
        await RecordAttemptAsync(user.Id, user.Email, null, ipAddress, userAgent, now, cancellationToken);

        return await IssueTokensAsync(user, tenant, Guid.CreateVersion7(), ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var hash = _refreshTokenGenerator.Hash(request.RefreshToken ?? string.Empty);

        var token = await _catalog.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null)
        {
            throw new InvalidCredentialsException();
        }

        if (!token.IsActive(now))
        {
            await RevokeFamilyAsync(token, now, "Reuse of an inactive refresh token detected.", cancellationToken);

            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; family {FamilyId} revoked.",
                token.UserId,
                token.FamilyId);

            throw new InvalidCredentialsException();
        }

        var user = await _catalog.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);

        var tenant = user is null
            ? null
            : await _catalog.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        if (user is null || tenant is null || tenant.Status != TenantStatus.Active || user.IsLockedOut(now))
        {
            await RevokeFamilyAsync(token, now, "User or tenant is no longer eligible.", cancellationToken);

            throw new InvalidCredentialsException();
        }

        return await IssueTokensAsync(user, tenant, token.FamilyId, ipAddress, cancellationToken, token, now);
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var hash = _refreshTokenGenerator.Hash(request.RefreshToken ?? string.Empty);

        var token = await _catalog.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null)
        {
            return;
        }

        await RevokeFamilyAsync(token, now, "Signed out.", cancellationToken);
    }

    private async Task<TwoFactorChallengeResponse> IssueTwoFactorChallengeAsync(
        User user,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var outstanding = await _catalog.UserTokens
            .Where(t => t.UserId == user.Id
                        && t.Purpose == UserTokenPurpose.TwoFactorChallenge
                        && t.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in outstanding)
        {
            token.Consume(now);
        }

        var value = _refreshTokenGenerator.Generate();

        var challenge = UserToken.Issue(
            user.Id,
            UserTokenPurpose.TwoFactorChallenge,
            _refreshTokenGenerator.Hash(value),
            now,
            TwoFactorChallengeLifetime);

        _catalog.UserTokens.Add(challenge);
        await _catalog.SaveChangesAsync(cancellationToken);

        return new TwoFactorChallengeResponse(value, challenge.ExpiresAtUtc);
    }

    private async Task<bool> TryConsumeRecoveryCodeAsync(
        Guid userId,
        string code,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var hash = _refreshTokenGenerator.Hash(code.ToUpperInvariant());

        var recoveryCode = await _catalog.RecoveryCodes
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CodeHash == hash, cancellationToken);

        if (recoveryCode is null || !recoveryCode.IsUsable)
        {
            return false;
        }

        recoveryCode.Use(now);

        _logger.LogWarning("Recovery code used for user {UserId}.", userId);

        return true;
    }

    private async Task RecordAttemptAsync(
        Guid? userId,
        string attemptedEmail,
        string? failureReason,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var attempt = failureReason is null && userId is not null
            ? LoginAttempt.Success(userId.Value, attemptedEmail, ipAddress, userAgent, now)
            : LoginAttempt.Failure(userId, attemptedEmail, failureReason ?? "Unknown.", ipAddress, userAgent, now);

        _catalog.LoginAttempts.Add(attempt);

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        Tenant tenant,
        Guid familyId,
        string? ipAddress,
        CancellationToken cancellationToken,
        RefreshToken? rotatedFrom = null,
        DateTimeOffset? nowOverride = null)
    {
        var now = nowOverride ?? _timeProvider.GetUtcNow();

        var accessToken = _accessTokenGenerator.Generate(user, tenant);

        var refreshTokenValue = _refreshTokenGenerator.Generate();

        var refreshToken = RefreshToken.Issue(
            user.Id,
            _refreshTokenGenerator.Hash(refreshTokenValue),
            familyId,
            now,
            RefreshTokenLifetime,
            ipAddress);

        _catalog.RefreshTokens.Add(refreshToken);

        rotatedFrom?.Revoke(now, "Rotated.", refreshToken.Id);

        await _catalog.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshTokenValue,
            refreshToken.ExpiresAtUtc);
    }

    private async Task RevokeFamilyAsync(
        RefreshToken token,
        DateTimeOffset now,
        string reason,
        CancellationToken cancellationToken)
    {
        var family = await _catalog.RefreshTokens
            .Where(t => t.FamilyId == token.FamilyId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var member in family)
        {
            member.Revoke(now, reason);
        }

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    private static string RequirePassword(string? password)
    {
        password ??= string.Empty;

        if (password.Length < MinimumPasswordLength)
        {
            throw new DomainException($"Password must be at least {MinimumPasswordLength} characters long.");
        }

        if (password.Length > MaximumPasswordLength)
        {
            throw new DomainException($"Password must not exceed {MaximumPasswordLength} characters.");
        }

        return password;
    }
}
