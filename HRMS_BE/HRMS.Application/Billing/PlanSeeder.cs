using HRMS.Application.Common.Interfaces;
using HRMS.Domain.Billing;
using HRMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Application.Billing;

public sealed class PlanSeeder : IPlanSeeder
{
    private static readonly (string Code, string Name, string Description, long Monthly, long Yearly, int? Seats, string[] Features)[] Catalogue =
    [
        (PlanCodes.Free, "Free", "For trying the system out.", 0, 0, 5, []),
        (PlanCodes.Basic, "Basic", "Core HR for small teams.", 4900, 49000, 50,
            [Features.Attendance]),
        (PlanCodes.Pro, "Pro", "Full HR including payroll.", 14900, 149000, 500,
            [Features.Attendance, Features.Payroll, Features.Reports]),
        (PlanCodes.Enterprise, "Enterprise", "Unlimited seats and API access.", 49900, 499000, null,
            [Features.Attendance, Features.Payroll, Features.Reports, Features.PublicApi])
    ];

    private readonly ICatalogDbContext _catalog;

    public PlanSeeder(ICatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    public async Task EnsurePlansAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _catalog.Plans.Select(p => p.Code).ToListAsync(cancellationToken);

        foreach (var entry in Catalogue)
        {
            if (existing.Contains(entry.Code))
            {
                continue;
            }

            var plan = Plan.Create(
                entry.Code, entry.Name, entry.Description, entry.Monthly, entry.Yearly, "USD", entry.Seats);

            _catalog.Plans.Add(plan);

            foreach (var feature in entry.Features)
            {
                _catalog.PlanFeatures.Add(PlanFeature.Include(plan.Id, feature));
            }
        }

        await _catalog.SaveChangesAsync(cancellationToken);
    }
}
