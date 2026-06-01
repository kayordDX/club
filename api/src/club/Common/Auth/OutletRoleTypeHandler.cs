using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Online.Common.Auth;

public class OutletRoleTypeHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<OutletRoleTypeRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OutletRoleTypeRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roles.Count == 0) return;

        // Extract route slug and check for slug-specific roles
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.GetRouteData() is { } routeData)
        {
            // Try to get slug from common route parameters
            var slug = routeData.Values["slug"]?.ToString()
                    ?? routeData.Values["id"]?.ToString()
                    ?? routeData.Values["outlet"]?.ToString();

            if (!string.IsNullOrEmpty(slug))
            {
                // Look for roles matching pattern: roleType:slug
                var requiredRoleWithSlug = $"{requirement.RoleType}:{slug}";
                if (roles.Contains(requiredRoleWithSlug))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
