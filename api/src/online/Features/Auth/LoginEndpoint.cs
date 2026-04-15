namespace Online.Features.Auth;

using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

public class LoginEndpoint : Endpoint<LoginRequest>
{
    public override void Configure()
    {
        Get("/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var returnUrl = req.ReturnUrl;

        // Security: only allow redirects back to the frontend origin to prevent open-redirect attacks.
        // Default to the frontend root when no returnUrl is supplied.
        var allowedOrigins = new[] { "http://localhost:5173", "http://localhost:4173" };
        if (string.IsNullOrEmpty(returnUrl) ||
            !allowedOrigins.Any(o => returnUrl.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
        {
            returnUrl = "http://localhost:5173";
        }

        await Send.ResultAsync(Results.Challenge(
            properties: new AuthenticationProperties
            {
                RedirectUri = returnUrl,
                IsPersistent = true,
            },
            authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]
        ));
    }
}
