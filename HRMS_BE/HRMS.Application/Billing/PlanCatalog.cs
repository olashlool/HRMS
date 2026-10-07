using HRMS.Application.Billing.Dtos;
using HRMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Billing;

public interface IPlanCatalog
{
    Task<IReadOnlyList<PlanResponse>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class PlanCatalog : IPlanCatalog
{
    private readonly ICatalogDbContext _catalog;
    private readonly IPlanSeeder _seeder;

    public PlanCatalog(ICatalogDbContext catalog, IPlanSeeder seeder)
    {
        _catalog = catalog;
        _seeder = seeder;
    }

    public async Task<IReadOnlyList<PlanResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _seeder.EnsurePlansAsync(cancellationToken);

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
}
