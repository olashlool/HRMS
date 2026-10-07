using HRMS.Domain.Entities.Enums;

namespace HRMS.Application.Billing.Dtos;

public sealed record PlanResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    long MonthlyPriceMinor,
    long YearlyPriceMinor,
    string Currency,
    int? MaxEmployees,
    IReadOnlyList<string> Features);

public sealed record SubscribeRequest(string PlanCode, BillingCycle BillingCycle, bool StartTrial);

public sealed record ChangePlanRequest(string PlanCode, BillingCycle BillingCycle);

public sealed record SubscriptionResponse(
    Guid Id,
    string PlanCode,
    string PlanName,
    BillingCycle BillingCycle,
    SubscriptionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CurrentPeriodEndUtc,
    DateTimeOffset? TrialEndsAtUtc,
    int? MaxEmployees,
    IReadOnlyList<string> Features);

public sealed record EntitlementResponse(
    bool HasSubscription,
    bool IsEntitled,
    string PlanCode,
    SubscriptionStatus Status,
    DateTimeOffset? CurrentPeriodEndUtc,
    int? MaxEmployees,
    int UsedEmployees,
    IReadOnlyList<string> Features);

public sealed record InvoiceResponse(
    Guid Id,
    string Number,
    long AmountMinor,
    string Currency,
    InvoiceStatus Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? PaidAtUtc);
