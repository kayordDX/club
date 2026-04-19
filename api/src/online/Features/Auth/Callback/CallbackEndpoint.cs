using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Client.AspNetCore;
using Online.Entities;
using Online.Services;

namespace Online.Features.Auth.Callback;

public class CallbackEndpoint(AccountService accountService, SignInManager<User> signInManager) : EndpointWithoutRequest
{
    private readonly AccountService _accountService = accountService;
    private readonly SignInManager<User> _signInManager = signInManager;

    public override void Configure()
    {
        Get("/auth/login/callback");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await _accountService.SyncUserAsync(result.Principal);

        var props = new AuthenticationProperties();
        props.StoreTokens(result.Properties.GetTokens());

        var backchannelExpiry = result.Properties.GetTokenValue("backchannel_access_token_expiration_date");
        if (!string.IsNullOrEmpty(backchannelExpiry))
        {
            props.UpdateTokenValue("expires_at", backchannelExpiry);
        }

        var accessToken = result.Properties.GetTokenValue("backchannel_access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            props.UpdateTokenValue("access_token", accessToken);
        }
        props.IsPersistent = true;

        var tokens = result.Properties.GetTokens();

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            result.Principal,
            props);

        var expiresAtString = result.Properties.GetTokenValue("backchannel_access_token_expiration_date");
        if (!DateTimeOffset.TryParse(expiresAtString, out var expiresAt))
        {
            expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        }

        var hasTokenExpires = expiresAt.UtcDateTime;
        HttpContext.Response.Cookies.Append("HAS_TOKEN", hasTokenExpires.ToString("o"), new CookieOptions
        {
            HttpOnly = false,
            Expires = hasTokenExpires,
            IsEssential = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });

        var redirectUrl = result.Properties?.RedirectUri ?? "/";
        HttpContext.Response.Redirect(redirectUrl);
        ResponseStarted = true;
        return;
    }
}
