using HRMS.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace HRMS.API.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var granted = context.User.Claims.Any(c =>
            c.Type == AccessTokenGenerator.PermissionClaim &&
            string.Equals(c.Value, requirement.Permission, StringComparison.Ordinal));

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
