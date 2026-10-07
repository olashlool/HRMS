using HRMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRMS.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class TenantRequiredAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var currentTenant = context.HttpContext.RequestServices.GetRequiredService<ICurrentTenant>();

        if (currentTenant.IsResolved)
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Tenant context required",
            Detail = "This endpoint operates on tenant data but no tenant was resolved for the request."
        })
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
