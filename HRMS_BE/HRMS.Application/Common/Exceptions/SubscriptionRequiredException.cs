namespace HRMS.Application.Common.Exceptions;

public sealed class SubscriptionRequiredException : Exception
{
    public SubscriptionRequiredException(string message) : base(message)
    {
    }
}

public sealed class PlanLimitExceededException : Exception
{
    public PlanLimitExceededException(string message) : base(message)
    {
    }
}
