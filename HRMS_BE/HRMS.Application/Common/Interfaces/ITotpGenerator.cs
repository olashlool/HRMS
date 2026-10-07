namespace HRMS.Application.Common.Interfaces;

public interface ITotpGenerator
{
    string CreateSecret();

    string BuildAuthenticatorUri(string issuer, string accountName, string secret);

    bool Verify(string secret, string code, DateTimeOffset now);
}
