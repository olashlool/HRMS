using HRMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace HRMS.Infrastructure.Authentication;

public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "HRMS.TwoFactorSecret.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
