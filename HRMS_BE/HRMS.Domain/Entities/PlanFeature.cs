using HRMS.Domain.Billing;
using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public sealed class PlanFeature : BaseEntity
{
    public const int FeatureMaxLength = 64;

    public Guid PlanId { get; private set; }
    public string Feature { get; private set; } = null!;

    private PlanFeature()
    {
    }

    private PlanFeature(Guid planId, string feature)
    {
        Id = Guid.CreateVersion7();
        PlanId = planId;
        Feature = feature;
    }

    public static PlanFeature Include(Guid planId, string feature)
    {
        if (planId == Guid.Empty)
        {
            throw new DomainException("Plan id is required.");
        }

        feature = feature?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!Features.IsKnown(feature))
        {
            throw new DomainException($"'{feature}' is not a recognised feature.");
        }

        return new PlanFeature(planId, feature);
    }
}
