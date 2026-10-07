namespace HRMS.Application.Common.Exceptions;

/// <summary>
/// The request is well formed but clashes with the current state, typically a
/// uniqueness violation. The API layer translates this into 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    /// <summary>
    /// Preserves the original database failure as InnerException so the real
    /// cause reaches the logs, while the caller sees a message that carries no
    /// schema details.
    /// </summary>
    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
