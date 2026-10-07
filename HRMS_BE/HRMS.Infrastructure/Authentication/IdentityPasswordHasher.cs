using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HRMS.Infrastructure.Authentication;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly User HashingContext = null!;

    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(HashingContext, password);

    public PasswordVerificationOutcome Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(HashingContext, hash, password);

        return result switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessButNeedsRehash,
            _ => PasswordVerificationOutcome.Failed
        };
    }
}
