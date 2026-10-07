namespace HRMS.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationOutcome Verify(string hash, string password);
}

public enum PasswordVerificationOutcome
{
    Failed = 0,
    Success = 1,
    SuccessButNeedsRehash = 2
}
