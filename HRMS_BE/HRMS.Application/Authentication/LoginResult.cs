using HRMS.Application.Authentication.Dtos;

namespace HRMS.Application.Authentication;

public sealed record LoginResult
{
    private LoginResult(AuthResponse? tokens, TwoFactorChallengeResponse? challenge)
    {
        Tokens = tokens;
        Challenge = challenge;
    }

    public AuthResponse? Tokens { get; }

    public TwoFactorChallengeResponse? Challenge { get; }

    public bool RequiresTwoFactor => Challenge is not null;

    public static LoginResult Authenticated(AuthResponse tokens) => new(tokens, null);

    public static LoginResult TwoFactorRequired(TwoFactorChallengeResponse challenge) => new(null, challenge);
}
