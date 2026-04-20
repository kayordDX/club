using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Client;

namespace Online.Services;

public class TokenRefreshService(OpenIddictClientService clientService)
{
    private const string HasTokenCookieName = "HAS_TOKEN";

    private readonly OpenIddictClientService _clientService = clientService;

    public async Task RefreshIfExpiredAsync(CookieValidatePrincipalContext context)
    {
        var expiresAtClaim = context.Properties.GetTokenValue("backchannel_access_token_expiration_date");
        if (string.IsNullOrEmpty(expiresAtClaim))
        {
            await Logout(context);
            return;
        }

        var expiresAt = DateTimeOffset.Parse(expiresAtClaim);
        // If the access token is not expiring within the next 2 minutes, do nothing
        if (expiresAt > DateTimeOffset.UtcNow.AddMinutes(2)) return;

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            await Logout(context);
            return;
        }

        try
        {
            // Use OpenIddict Client to call the Identity /connect/token endpoint
            var result = await _clientService.AuthenticateWithRefreshTokenAsync(new()
            {
                RefreshToken = refreshToken
            });
            context.Properties.UpdateTokenValue("backchannel_access_token", result.AccessToken);
            context.Properties.UpdateTokenValue("refresh_token", result.RefreshToken);
            if (!result.AccessTokenExpirationDate.HasValue)
            {
                throw new InvalidOperationException("Token response is missing 'AccessTokenExpirationDate' information.");
            }

            var accessTokenExpirationDate = result.AccessTokenExpirationDate.Value;
            context.Properties.UpdateTokenValue(
                "backchannel_access_token_expiration_date",
                accessTokenExpirationDate.ToString("o"));

            WriteHasTokenCookie(context, DateTimeOffset.UtcNow.AddDays(14));
            context.ShouldRenew = true;
        }
        catch
        {
            await Logout(context);
        }
    }

    private static void WriteHasTokenCookie(CookieValidatePrincipalContext context, DateTimeOffset expiresAt)
    {
        context.HttpContext.Response.Cookies.Append(HasTokenCookieName, "true", new CookieOptions
        {
            HttpOnly = false,
            Expires = expiresAt.UtcDateTime,
            IsEssential = true,
            Secure = context.HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });
    }

    private static async Task Logout(CookieValidatePrincipalContext context)
    {
        context.HttpContext.Response.Cookies.Delete(HasTokenCookieName);
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }
}
