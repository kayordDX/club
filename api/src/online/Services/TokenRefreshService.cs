using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Client;

namespace Online.Services;

public class TokenRefreshService(OpenIddictClientService clientService)
{
    private readonly OpenIddictClientService _clientService = clientService;

    public async Task RefreshIfExpiredAsync(CookieValidatePrincipalContext context)
    {
        // Get the 'expires_at' value we stored during initial login
        var expiresAtClaim = context.Properties.GetTokenValue("backchannel_access_token_expiration_date");
        if (string.IsNullOrEmpty(expiresAtClaim)) return;

        var expiresAt = DateTimeOffset.Parse(expiresAtClaim);

        // If the access token has more than 2 minutes of life, we are fine.
        if (expiresAt > DateTimeOffset.UtcNow.AddMinutes(2)) return;

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken)) return;

        try
        {
            // Use OpenIddict Client to call the Identity /connect/token endpoint
            var result = await _clientService.AuthenticateWithRefreshTokenAsync(new()
            {
                RefreshToken = refreshToken
            });

            // Update the Access Token and the Refresh Token (Identity might rotate it)
            context.Properties.UpdateTokenValue("access_token", result.AccessToken);
            context.Properties.UpdateTokenValue("refresh_token", result.RefreshToken);

            // OpenIddict's refresh result doesn't expose the raw token response here,
            // so keep the expiry in sync using the same lifetime as the issued access token.
            var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(3600);

            context.Properties.UpdateTokenValue("expires_at", newExpiresAt.ToString("o"));

            // CRITICAL: Tells the Cookie Middleware to write the updated tokens back to the browser
            context.ShouldRenew = true;
        }
        catch
        {
            // If refresh fails (e.g. user revoked, password changed), kill the session
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
