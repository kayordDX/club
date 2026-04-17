namespace Online.Features.Auth;

using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Client.AspNetCore;

public class LoginEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var returnUrl = HttpContext.Request.Query["returnUrl"].ToString();
        var properties = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
        };

        await HttpContext.ChallengeAsync(
            OpenIddictClientAspNetCoreDefaults.AuthenticationScheme,
            properties);
        ResponseStarted = true;
    }
}
