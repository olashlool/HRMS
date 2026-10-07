namespace HRMS.Domain.Entities.Enums;

public enum BillingCycle
{
    Monthly = 1,
    Yearly = 2
}

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Expired = 5
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Void = 4
}
