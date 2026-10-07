using HRMS.Application.Billing.Dtos;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Billing;
using HRMS.Domain.Entities;
using HRMS.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Billing;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly ICatalogDbContext _catalog;
    private readonly ITenantDbContext _tenantDb;
    private readonly ICurrentTenant _currentTenant;
    private readonly IEntitlementResolver _entitlements;
    private readonly TimeProvider _timeProvider;

    public SubscriptionService(
        ICatalogDbContext catalog,
        ITenantDbContext tenantDb,
        ICurrentTenant currentTenant,
        IEntitlementResolver entitlements,
        TimeProvider timeProvider)
    {
        _catalog = catalog;
        _tenantDb = tenantDb;
        _currentTenant = currentTenant;
        _entitlements = entitlements;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<PlanResponse>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _catalog.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPriceMinor)
            .ToListAsync(cancellationToken);

        var planIds = plans.Select(p => p.Id).ToList();

        var features = await _catalog.PlanFeatures
            .AsNoTracking()
            .Where(f => planIds.Contains(f.PlanId))
            .ToListAsync(cancellationToken);

        return plans
            .Select(p => new PlanResponse(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.MonthlyPriceMinor,
                p.YearlyPriceMinor,
                p.Currency,
                p.MaxEmployees,
                features.Where(f => f.PlanId == p.Id).Select(f => f.Feature).Order().ToList()))
            .ToList();
    }

    public async Task<SubscriptionResponse> SubscribeAsync(
        SubscribeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;
        var now = _timeProvider.GetUtcNow();
        var plan = await RequirePlanAsync(request.PlanCode, cancellationToken);

        var current = await _catalog.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is not null && current.IsEntitled(now))
        {
            throw new ConflictException(
                "This tenant already has an active subscription; change the plan instead of subscribing again.");
        }

        var subscription = request.StartTrial
            ? Subscription.StartTrial(tenantId, plan.Id, request.BillingCycle, now)
            : Subscription.StartPaid(tenantId, plan.Id, request.BillingCycle, now);

        _catalog.Subscriptions.Add(subscription);

        if (!request.StartTrial)
        {
            await IssueInvoiceAsync(tenantId, subscription, plan, now, cancellationToken);
        }

        await _catalog.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(subscription, plan, cancellationToken);
    }

    public async Task<SubscriptionResponse> ChangePlanAsync(
        ChangePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;
        var now = _timeProvider.GetUtcNow();
        var plan = await RequirePlanAsync(request.PlanCode, cancellationToken);
        var subscription = await RequireSubscriptionAsync(tenantId, cancellationToken);

        if (plan.MaxEmployees is { } limit)
        {
            var used = await _tenantDb.Employees.CountAsync(cancellationToken);

            if (used > limit)
            {
                throw new PlanLimitExceededException(
                    $"This tenant has {used} employees, which exceeds the {plan.Name} limit of {limit}. " +
                    "Remove employees before downgrading.");
            }
        }

        subscription.ChangePlan(plan.Id, request.BillingCycle, now);

        await IssueInvoiceAsync(tenantId, subscription, plan, now, cancellationToken);

        await _catalog.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(subscription, plan, cancellationToken);
    }

    public async Task<SubscriptionResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;
        var subscription = await RequireSubscriptionAsync(tenantId, cancellationToken);

        var plan = await _catalog.Plans
            .AsNoTracking()
            .FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);

        return await ToResponseAsync(subscription, plan, cancellationToken);
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;
        var subscription = await RequireSubscriptionAsync(tenantId, cancellationToken);

        subscription.Cancel(_timeProvider.GetUtcNow());

        await _catalog.SaveChangesAsync(cancellationToken);
    }

    public async Task<EntitlementResponse> GetEntitlementsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;
        var entitlements = await _entitlements.ResolveAsync(tenantId, cancellationToken);
        var used = await _tenantDb.Employees.CountAsync(cancellationToken);

        return new EntitlementResponse(
            entitlements.HasSubscription,
            entitlements.IsEntitled,
            entitlements.PlanCode,
            entitlements.Status,
            entitlements.CurrentPeriodEndUtc,
            entitlements.MaxEmployees,
            used,
            entitlements.Features.Order().ToList());
    }

    public async Task<IReadOnlyList<InvoiceResponse>> ListInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenant.TenantId;

        return await _catalog.Invoices
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Select(i => new InvoiceResponse(
                i.Id, i.Number, i.AmountMinor, i.Currency, i.Status, i.IssuedAtUtc, i.PaidAtUtc))
            .ToListAsync(cancellationToken);
    }

    private async Task IssueInvoiceAsync(
        Guid tenantId,
        Subscription subscription,
        Plan plan,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var count = await _catalog.Invoices.CountAsync(i => i.TenantId == tenantId, cancellationToken);
        var number = $"INV-{now:yyyyMM}-{count + 1:D4}";

        _catalog.Invoices.Add(Invoice.Issue(
            tenantId,
            subscription.Id,
            number,
            plan.PriceFor(subscription.BillingCycle),
            plan.Currency,
            now));
    }

    private async Task<Plan> RequirePlanAsync(string? code, CancellationToken cancellationToken)
    {
        var normalized = code?.Trim().ToLowerInvariant() ?? string.Empty;

        var plan = await _catalog.Plans
            .FirstOrDefaultAsync(p => p.Code == normalized && p.IsActive, cancellationToken);

        return plan ?? throw new NotFoundException(nameof(Plan), normalized);
    }

    private async Task<Subscription> RequireSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var subscription = await _catalog.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return subscription ?? throw new NotFoundException(nameof(Subscription), tenantId);
    }

    private async Task<SubscriptionResponse> ToResponseAsync(
        Subscription subscription,
        Plan plan,
        CancellationToken cancellationToken)
    {
        var features = await _catalog.PlanFeatures
            .AsNoTracking()
            .Where(f => f.PlanId == plan.Id)
            .Select(f => f.Feature)
            .ToListAsync(cancellationToken);

        return new SubscriptionResponse(
            subscription.Id,
            plan.Code,
            plan.Name,
            subscription.BillingCycle,
            subscription.Status,
            subscription.StartedAtUtc,
            subscription.CurrentPeriodEndUtc,
            subscription.TrialEndsAtUtc,
            plan.MaxEmployees,
            features.Order().ToList());
    }
}
