namespace HRMS.Application.Common.Exceptions;

public sealed class InvalidTokenException : Exception
{
    public InvalidTokenException()
        : base("The token is invalid, expired, or has already been used.")
    {
    }
}
