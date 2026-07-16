using System.Security.Claims;
using FluentValidation.Results;

namespace Club.Common;

public static class Helpers
{
    public static Guid? GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public static Guid GetUserId(HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        if (userId == null)
        {
            var failure = new ValidationFailure("User", "User ID is missing or invalid in the current context.");
            throw new ValidationFailureException([failure], "Authentication validation failed.");
        }
        return userId.Value;
    }

    public static string? GetCurrentUsername(HttpContext httpContext)
    {
        return httpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? httpContext.User.FindFirst("preferred_username")?.Value;
    }

    public static string GetUsername(HttpContext httpContext)
    {
        var username = GetCurrentUsername(httpContext);

        if (string.IsNullOrWhiteSpace(username))
        {
            var failure = new ValidationFailure("Username", "Username could not be determined from the session context.");
            throw new ValidationFailureException(new[] { failure }, "Authentication validation failed.");
        }

        return username;
    }
}
