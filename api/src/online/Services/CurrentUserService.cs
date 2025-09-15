using Microsoft.AspNetCore.Identity;
using Online.Entities;

namespace Online.Services;

public class CurrentUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user == null ? null : await _userManager.GetUserAsync(user);
    }
}