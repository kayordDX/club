using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Extensions;
using Online.Entities;
using Online.Services;
using OpenIddict.Client.AspNetCore;

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
        // 1. Validate the OIDC response from the Identity Server
        var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await _accountService.SyncUserAsync(result.Principal);

        var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await _signInManager.UserManager.FindByEmailAsync(email);
        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // 2. Issue the registered Identity application cookie through Identity so only
        // the configured application cookie is created.
        // await _signInManager.SignInAsync(user, isPersistent: false);

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            result.Principal,
            result.Properties);

        // 3. Clear any temporary external cookie after the callback flow completes.
        // await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        // 3. Issue a frontend-visible marker cookie so the SPA can detect the session.
        var expiresAt = result.Properties?.ExpiresUtc?.UtcDateTime ?? DateTime.UtcNow.AddHours(1);
        HttpContext.Response.Cookies.Append("HAS_TOKEN", expiresAt.ToString("o"), new CookieOptions
        {
            HttpOnly = false,
            Expires = expiresAt,
            IsEssential = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });

        string redirectUrl = result.Properties?.RedirectUri ?? "/";

        HttpContext.Response.Redirect(redirectUrl);
        ResponseStarted = true;
        return;
    }
}
