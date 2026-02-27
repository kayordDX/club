
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Online.Services;

namespace Online.Features.Account.Login.Google.Callback;

public class Endpoint(AccountService accountService) : Endpoint<GoogleLoginCallbackRequest>
{
    private readonly AccountService _accountService = accountService;

    public override void Configure()
    {
        Get("/account/login/google/callback");
        Description(x => x.WithName("GoogleLoginCallback").ExcludeFromDescription());
        AllowAnonymous();
    }

    public override async Task HandleAsync(GoogleLoginCallbackRequest r, CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        await _accountService.LoginWithGoogleAsync(result.Principal);
        await Send.ResultAsync(Results.Redirect(r.ReturnUrl));
    }
}
