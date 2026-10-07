namespace HRMS.Application.Common.Interfaces;

public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}
