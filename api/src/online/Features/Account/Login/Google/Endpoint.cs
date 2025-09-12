
using Microsoft.AspNetCore.Authentication;

namespace Online.Features.Account.Login.Google;

public class Endpoint : Endpoint<Request>
{
    public override void Configure()
    {
        Get("/account/login/google");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = $"/account/login/google/callback?returnUrl={r.ReturnUrl}"
        };
        await Send.ResultAsync(Results.Challenge(props, ["Google"]));
    }
}