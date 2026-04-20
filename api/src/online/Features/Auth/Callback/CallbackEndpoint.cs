using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Client.AspNetCore;
using Online.Entities;
using Online.Services;

namespace Online.Features.Auth.Callback;

public class CallbackEndpoint(AccountService accountService) : EndpointWithoutRequest
{
    private readonly AccountService _accountService = accountService;

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

        // var props = new AuthenticationProperties();
        // props.StoreTokens(result.Properties.GetTokens());
        // props.IsPersistent = true;

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            result.Principal,
            result.Properties);

        var expiry = result.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddDays(14);
        HttpContext.Response.Cookies.Append("HAS_TOKEN", "true", new CookieOptions
        {
            HttpOnly = false,
            Expires = expiry,
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
