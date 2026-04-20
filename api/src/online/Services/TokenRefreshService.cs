using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Client;

namespace Online.Services;

public class TokenRefreshService(OpenIddictClientService clientService, ILogger<TokenRefreshService> logger)
{
    private readonly OpenIddictClientService _clientService = clientService;
    private readonly ILogger<TokenRefreshService> _logger = logger;

    public async Task RefreshIfExpiredAsync(CookieValidatePrincipalContext context)
    {
        // Get the 'expires_at' value we stored during initial login
        var expiresAtClaim = context.Properties.GetTokenValue("backchannel_access_token_expiration_date");
        if (string.IsNullOrEmpty(expiresAtClaim)) return;

        var expiresAt = DateTimeOffset.Parse(expiresAtClaim);

        // If the access token has more than 2 minutes of life, we are fine.
        if (expiresAt > DateTimeOffset.UtcNow.AddMinutes(5)) return;

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken)) return;

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
            context.Properties.UpdateTokenValue("backchannel_access_token_expiration_date", result.AccessTokenExpirationDate.Value.ToString("o"));
            context.ShouldRenew = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh access token. User will be signed out.");
            // If refresh fails (e.g. user revoked, password changed), kill the session
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    }
}
