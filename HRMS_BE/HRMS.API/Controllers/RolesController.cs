using HRMS.API.Authorization;
using HRMS.API.Filters;
using HRMS.Application.Authorization;
using HRMS.Application.Authorization.Dtos;
using HRMS.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[TenantRequired]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleAdministrationService _roles;

    public RolesController(IRoleAdministrationService roles)
    {
        _roles = roles;
    }

    [HttpGet]
    [HasPermission(Permissions.Roles.Read)]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _roles.ListAsync(cancellationToken));
    }

    [HttpGet("permissions")]
    [HasPermission(Permissions.Roles.Read)]
    public ActionResult<IReadOnlyList<string>> AvailablePermissions()
    {
        return Ok(Permissions.TenantScoped.Order().ToList());
    }

    [HttpPost]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<ActionResult<RoleResponse>> Create(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        return await _roles.CreateAsync(CurrentUserId(), request, cancellationToken);
    }

    [HttpPut("{roleId:guid}/permissions")]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<ActionResult<RoleResponse>> SetPermissions(
        Guid roleId,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        return await _roles.SetPermissionsAsync(CurrentUserId(), roleId, request, cancellationToken);
    }

    [HttpPost("users/{userId:guid}/roles")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _roles.AssignRoleAsync(CurrentUserId(), userId, request.RoleId, cancellationToken);

        return NoContent();
    }

    [HttpPut("users/{userId:guid}/permissions")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> SetUserPermission(
        Guid userId,
        SetUserPermissionRequest request,
        CancellationToken cancellationToken)
    {
        await _roles.SetUserPermissionOverrideAsync(CurrentUserId(), userId, request, cancellationToken);

        return NoContent();
    }

    [HttpGet("users/{userId:guid}/permissions")]
    [HasPermission(Permissions.Users.Read)]
    public async Task<ActionResult<EffectivePermissionsResponse>> EffectivePermissions(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _roles.GetEffectivePermissionsAsync(userId, cancellationToken);
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? Guid.Empty.ToString());
}
