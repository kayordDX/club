using Microsoft.AspNetCore.Authorization;

namespace Club.Common.Auth;

public static class AuthorizationOptionsExtensions
{
    public static AuthorizationOptions AddFacilityRolePolicy(this AuthorizationOptions options, string roleName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        options.AddPolicy(roleName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new FacilityRoleRequirement(roleName));
        });

        return options;
    }
}
