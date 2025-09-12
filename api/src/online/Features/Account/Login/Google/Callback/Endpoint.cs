
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity.Data;
using Online.Services;

namespace Online.Features.Account.Login.Google.Callback;

public class Endpoint : Endpoint<Request>
{
    private readonly AccountService _accountService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Endpoint(AccountService accountService, IHttpContextAccessor httpContextAccessor)
    {
        _accountService = accountService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Configure()
    {
        Get("/account/login/google/callback");
        Description(x => x.WithName("GoogleLoginCallback"));
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
        var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            await Send.UnauthorizedAsync();
            return;
        }
        await _accountService.LoginWithGoogleAsync(result.Principal);
        await Send.RedirectAsync(r.ReturnUrl);
    }
}