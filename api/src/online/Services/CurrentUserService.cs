using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Online.Entities;

namespace Online.Services;

public class CurrentUserService(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid? GetId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user == null ? null : Guid.Parse(_userManager.GetUserId(user) ?? Guid.Empty.ToString());
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user == null ? null : await _userManager.GetUserAsync(user);
    }
}
