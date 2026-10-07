using HRMS.Domain.Common;
using HRMS.Domain.Entities.Enums;

namespace HRMS.Domain.Entities;

public sealed class Subscription : AuditableEntity
{
    public const int ExternalIdMaxLength = 128;

    public static readonly TimeSpan TrialLength = TimeSpan.FromDays(14);
    public static readonly TimeSpan PastDueGrace = TimeSpan.FromDays(7);

    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public BillingCycle BillingCycle { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset CurrentPeriodEndUtc { get; private set; }
    public DateTimeOffset? TrialEndsAtUtc { get; private set; }
    public DateTimeOffset? CanceledAtUtc { get; private set; }
    public string? ExternalSubscriptionId { get; private set; }

    private Subscription()
    {
    }

    private Subscription(
        Guid tenantId,
        Guid planId,
        BillingCycle billingCycle,
        SubscriptionStatus status,
        DateTimeOffset startedAtUtc,
        DateTimeOffset currentPeriodEndUtc,
        DateTimeOffset? trialEndsAtUtc)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        PlanId = planId;
        BillingCycle = billingCycle;
        Status = status;
        StartedAtUtc = startedAtUtc;
        CurrentPeriodEndUtc = currentPeriodEndUtc;
        TrialEndsAtUtc = trialEndsAtUtc;
    }

    public static Subscription StartTrial(Guid tenantId, Guid planId, BillingCycle cycle, DateTimeOffset now)
    {
        Guard(tenantId, planId, cycle);

        var trialEnd = now.Add(TrialLength);

        return new Subscription(tenantId, planId, cycle, SubscriptionStatus.Trialing, now, trialEnd, trialEnd);
    }

    public static Subscription StartPaid(Guid tenantId, Guid planId, BillingCycle cycle, DateTimeOffset now)
    {
        Guard(tenantId, planId, cycle);

        return new Subscription(tenantId, planId, cycle, SubscriptionStatus.Active, now, NextPeriodEnd(now, cycle), null);
    }

    public bool IsEntitled(DateTimeOffset now) => Status switch
    {
        SubscriptionStatus.Trialing => CurrentPeriodEndUtc > now,
        SubscriptionStatus.Active => CurrentPeriodEndUtc > now,
        SubscriptionStatus.PastDue => CurrentPeriodEndUtc.Add(PastDueGrace) > now,
        _ => false
    };

    public void ChangePlan(Guid planId, BillingCycle cycle, DateTimeOffset now)
    {
        if (planId == Guid.Empty)
        {
            throw new DomainException("Plan id is required.");
        }

        if (Status is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
        {
            throw new DomainException("A cancelled subscription cannot change plan; start a new one instead.");
        }

        PlanId = planId;
        BillingCycle = cycle;
        CurrentPeriodEndUtc = NextPeriodEnd(now, cycle);

        if (Status == SubscriptionStatus.Trialing)
        {
            Status = SubscriptionStatus.Active;
            TrialEndsAtUtc = null;
        }
    }

    public void Renew(DateTimeOffset now)
    {
        if (Status is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
        {
            throw new DomainException("A cancelled subscription cannot be renewed.");
        }

        Status = SubscriptionStatus.Active;
        TrialEndsAtUtc = null;
        CurrentPeriodEndUtc = NextPeriodEnd(now, BillingCycle);
    }

    public void MarkPastDue()
    {
        if (Status is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
        {
            throw new DomainException("A cancelled subscription cannot become past due.");
        }

        Status = SubscriptionStatus.PastDue;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == SubscriptionStatus.Canceled)
        {
            return;
        }

        Status = SubscriptionStatus.Canceled;
        CanceledAtUtc = now;
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
    }

    public void LinkToProvider(string externalSubscriptionId)
    {
        externalSubscriptionId = externalSubscriptionId?.Trim() ?? string.Empty;

        if (externalSubscriptionId.Length == 0 || externalSubscriptionId.Length > ExternalIdMaxLength)
        {
            throw new DomainException("The provider subscription id is required and must not exceed 128 characters.");
        }

        ExternalSubscriptionId = externalSubscriptionId;
    }

    private static DateTimeOffset NextPeriodEnd(DateTimeOffset from, BillingCycle cycle) =>
        cycle == BillingCycle.Yearly ? from.AddYears(1) : from.AddMonths(1);

    private static void Guard(Guid tenantId, Guid planId, BillingCycle cycle)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        if (planId == Guid.Empty)
        {
            throw new DomainException("Plan id is required.");
        }

        if (!Enum.IsDefined(cycle))
        {
            throw new DomainException("Billing cycle is not a recognised value.");
        }
    }
}
