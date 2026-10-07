using HRMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMS.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresFeatureAttribute : Attribute, IAsyncResourceFilter
{
    private readonly string _feature;

    public RequiresFeatureAttribute(string feature)
    {
        _feature = feature;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var currentTenant = services.GetRequiredService<ICurrentTenant>();

        if (!currentTenant.IsResolved)
        {
            context.Result = Problem(
                StatusCodes.Status400BadRequest,
                "Tenant context required",
                "This endpoint operates on tenant data but no tenant was resolved for the request.");

            return;
        }

        var resolver = services.GetRequiredService<IEntitlementResolver>();
        var entitlements = await resolver.ResolveAsync(currentTenant.TenantId, context.HttpContext.RequestAborted);

        if (!entitlements.IsEntitled)
        {
            context.Result = Problem(
                StatusCodes.Status402PaymentRequired,
                "Subscription required",
                "This tenant does not have an active subscription.");

            return;
        }

        if (!entitlements.Includes(_feature))
        {
            context.Result = Problem(
                StatusCodes.Status402PaymentRequired,
                "Plan upgrade required",
                $"The '{entitlements.PlanName}' plan does not include the '{_feature}' feature.");

            return;
        }

        await next();
    }

    private static ObjectResult Problem(int statusCode, string title, string detail) =>
        new(new ProblemDetails { Status = statusCode, Title = title, Detail = detail })
        {
            StatusCode = statusCode
        };
}
