using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Billing;

public sealed record TenantEntitlements(
    bool HasSubscription,
    bool IsEntitled,
    string PlanCode,
    string PlanName,
    SubscriptionStatus Status,
    DateTimeOffset? CurrentPeriodEndUtc,
    int? MaxEmployees,
    IReadOnlySet<string> Features)
{
    public static TenantEntitlements None { get; } = new(
        false,
        false,
        string.Empty,
        string.Empty,
        SubscriptionStatus.Expired,
        null,
        0,
        new HashSet<string>(StringComparer.Ordinal));

    public bool Includes(string feature) => IsEntitled && Features.Contains(feature);
}

public sealed class EntitlementResolver : IEntitlementResolver
{
    private readonly ICatalogDbContext _catalog;
    private readonly TimeProvider _timeProvider;

    private TenantEntitlements? _cachedForRequest;
    private Guid _cachedTenantId;

    public EntitlementResolver(ICatalogDbContext catalog, TimeProvider timeProvider)
    {
        _catalog = catalog;
        _timeProvider = timeProvider;
    }

    public async Task<TenantEntitlements> ResolveAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (_cachedForRequest is not null && _cachedTenantId == tenantId)
        {
            return _cachedForRequest;
        }

        var now = _timeProvider.GetUtcNow();

        var record = await _catalog.Subscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAtUtc)
            .Join(
                _catalog.Plans.AsNoTracking(),
                s => s.PlanId,
                p => p.Id,
                (s, p) => new { Subscription = s, Plan = p })
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            _cachedTenantId = tenantId;
            _cachedForRequest = TenantEntitlements.None;

            return _cachedForRequest;
        }

        var features = await _catalog.PlanFeatures
            .AsNoTracking()
            .Where(f => f.PlanId == record.Plan.Id)
            .Select(f => f.Feature)
            .ToListAsync(cancellationToken);

        _cachedTenantId = tenantId;
        _cachedForRequest = new TenantEntitlements(
            true,
            record.Subscription.IsEntitled(now),
            record.Plan.Code,
            record.Plan.Name,
            record.Subscription.Status,
            record.Subscription.CurrentPeriodEndUtc,
            record.Plan.MaxEmployees,
            new HashSet<string>(features, StringComparer.Ordinal));

        return _cachedForRequest;
    }
}
