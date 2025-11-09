using System.Security.Claims;

namespace Online.Common;

public static class Helpers
{
    public static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}