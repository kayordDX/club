using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Online.Entities;

namespace Online.Features.Account.Logout;

public class Endpoint(SignInManager<User> signInManager, IHttpContextAccessor httpContextAccessor) : EndpointWithoutRequest<bool>
{
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public override void Configure()
    {
        Post("/account/logout");
        Description(x => x.WithName("Logout"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("HAS_TOKEN");
        if (_httpContextAccessor.HttpContext is not null)
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
        await Send.OkAsync(true, ct);
    }
}
