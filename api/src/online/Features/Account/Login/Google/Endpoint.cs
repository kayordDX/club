
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Org.BouncyCastle.Asn1.Cmp;

namespace Online.Features.Account.Login.Google;

public class Endpoint : Endpoint<Request>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Endpoint(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Configure()
    {
        Get("/account/login/google");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            await Send.UnauthorizedAsync();
            return;
        }

        await httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties
            {
                RedirectUri = "/account/login/google/callback"
            });

        // var props = new AuthenticationProperties
        // {
        //     RedirectUri = "/account/login/google/callback"
        // };
        // Challenge(props, GoogleDefaults.AuthenticationScheme);
        // await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
    }
}