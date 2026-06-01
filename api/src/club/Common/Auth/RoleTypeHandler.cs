using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Online.Common.Auth;

public class RoleTypeHandler : AuthorizationHandler<RoleTypeRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleTypeRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roles.Count == 0) return;

        if (roles.Contains(requirement.RoleType))
        {
            context.Succeed(requirement);
        }
    }
}
