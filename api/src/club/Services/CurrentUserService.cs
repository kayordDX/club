using Microsoft.AspNetCore.Identity;
using Online.Entities;

namespace Online.Services;

public class CurrentUserService(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid? GetId()
    {
        return _httpContextAccessor.HttpContext?.User?.GetUserId();
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user == null ? null : await _userManager.GetUserAsync(user);
    }
}
