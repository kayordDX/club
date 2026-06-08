using Microsoft.AspNetCore.Authorization;
using Club.Data;

namespace Club.Common.Auth;

public class FacilityRoleHandler(
    UserStore userStore,
    ICurrentFacilityAccessor currentFacilityAccessor,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<FacilityRoleRequirement>
{
    private readonly UserStore _userStore = userStore;
    private readonly ICurrentFacilityAccessor _currentFacilityAccessor = currentFacilityAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FacilityRoleRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (!userId.HasValue)
        {
            return;
        }

        if (!_currentFacilityAccessor.TryGetFacilityId(out var facilityId))
        {
            return;
        }

        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var isInRole = await _userStore.IsInRoleAsync(userId.Value, requirement.RoleName, facilityId, cancellationToken);

        if (isInRole)
        {
            context.Succeed(requirement);
        }
    }
}
