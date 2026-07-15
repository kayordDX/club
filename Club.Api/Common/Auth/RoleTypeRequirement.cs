using Microsoft.AspNetCore.Authorization;

namespace Club.Common.Auth;

public class RoleTypeRequirement(string roleType) : IAuthorizationRequirement
{
    public string RoleType { get; set; } = roleType;
}
