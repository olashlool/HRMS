using HRMS.API.Authorization;
using HRMS.API.Filters;
using HRMS.Application.Billing;
using HRMS.Application.Billing.Dtos;
using HRMS.Domain.Authorization;
using HRMS.Domain.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class PlansController : ControllerBase
{
    private readonly IPlanCatalog _planCatalog;

    public PlansController(IPlanCatalog planCatalog)
    {
        _planCatalog = planCatalog;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PlanResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _planCatalog.ListAsync(cancellationToken));
    }
}

[ApiController]
[Route("api/[controller]")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionsController(ISubscriptionService subscriptions)
    {
        _subscriptions = subscriptions;
    }

    [HttpGet("current")]
    [TenantRequired]
    [HasPermission(Permissions.Users.Read)]
    public async Task<ActionResult<SubscriptionResponse>> Current(CancellationToken cancellationToken)
    {
        return await _subscriptions.GetCurrentAsync(cancellationToken);
    }

    [HttpGet("entitlements")]
    [TenantRequired]
    [Authorize]
    public async Task<ActionResult<EntitlementResponse>> Entitlements(CancellationToken cancellationToken)
    {
        return await _subscriptions.GetEntitlementsAsync(cancellationToken);
    }

    [HttpPost]
    [TenantRequired]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<SubscriptionResponse>> Subscribe(
        SubscribeRequest request,
        CancellationToken cancellationToken)
    {
        return await _subscriptions.SubscribeAsync(request, cancellationToken);
    }

    [HttpPut("plan")]
    [TenantRequired]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<SubscriptionResponse>> ChangePlan(
        ChangePlanRequest request,
        CancellationToken cancellationToken)
    {
        return await _subscriptions.ChangePlanAsync(request, cancellationToken);
    }

    [HttpDelete]
    [TenantRequired]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        await _subscriptions.CancelAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("invoices")]
    [TenantRequired]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<IReadOnlyList<InvoiceResponse>>> Invoices(CancellationToken cancellationToken)
    {
        return Ok(await _subscriptions.ListInvoicesAsync(cancellationToken));
    }
}

[ApiController]
[Route("api/payroll")]
[TenantRequired]
[RequiresFeature(Features.Payroll)]
public sealed class PayrollController : ControllerBase
{
    [HttpGet("summary")]
    [HasPermission(Permissions.Payroll.Read)]
    public IActionResult Summary()
    {
        return Ok(new { message = "Payroll module reached: the plan includes it and the caller may read it." });
    }
}
