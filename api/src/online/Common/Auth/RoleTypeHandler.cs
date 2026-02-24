using Microsoft.AspNetCore.Authorization;
using Online.Data;

namespace Online.Common.Auth;

public class RoleTypeHandler(UserStore userStore) : AuthorizationHandler<RoleTypeRequirement>
{
    private readonly UserStore _userStore = userStore;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleTypeRequirement requirement)
    {
        var roles = await _userStore.GetRolesAsync(context);
        if (roles.Count == 0) return;

        if (roles.Contains(requirement.RoleType))
        {
            context.Succeed(requirement);
        }
    }
}
