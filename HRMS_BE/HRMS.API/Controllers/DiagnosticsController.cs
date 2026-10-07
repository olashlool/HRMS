using HRMS.API.Filters;
using HRMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[DevelopmentOnly]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly ICurrentTenant _currentTenant;

    public DiagnosticsController(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }

    [HttpGet("current-tenant")]
    public IActionResult CurrentTenant()
    {
        if (!_currentTenant.IsResolved)
        {
            return Ok(new { resolved = false });
        }

        return Ok(new
        {
            resolved = true,
            tenantId = _currentTenant.TenantId,
            slug = _currentTenant.Slug
        });
    }
}
