using HRMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMS.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ActiveSubscriptionRequiredAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var currentTenant = services.GetRequiredService<ICurrentTenant>();

        if (!currentTenant.IsResolved)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tenant context required",
                Detail = "This endpoint operates on tenant data but no tenant was resolved for the request."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };

            return;
        }

        var resolver = services.GetRequiredService<IEntitlementResolver>();
        var entitlements = await resolver.ResolveAsync(currentTenant.TenantId, context.HttpContext.RequestAborted);

        if (!entitlements.IsEntitled)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status402PaymentRequired,
                Title = "Subscription required",
                Detail = entitlements.HasSubscription
                    ? $"The subscription is {entitlements.Status} and no longer grants access."
                    : "This tenant has no subscription."
            })
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };

            return;
        }

        await next();
    }
}
