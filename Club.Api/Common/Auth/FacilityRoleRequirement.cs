using Microsoft.AspNetCore.Authorization;

namespace Club.Common.Auth;

public class FacilityRoleRequirement(string roleName) : IAuthorizationRequirement
{
    public string RoleName { get; } = roleName;
}
