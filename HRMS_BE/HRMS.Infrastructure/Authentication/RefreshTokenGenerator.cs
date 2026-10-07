using System.Security.Cryptography;
using System.Text;
using HRMS.Application.Common.Interfaces;

namespace HRMS.Infrastructure.Authentication;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenBytes = 32;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytes);

        return Base64UrlEncode(bytes);
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
