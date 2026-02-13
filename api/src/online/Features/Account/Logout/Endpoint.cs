using Online.Services;

namespace Online.Features.Account.Logout;

public class Endpoint(AccountService accountService) : EndpointWithoutRequest<bool>
{
    private readonly AccountService _accountService = accountService;

    public override void Configure()
    {
        Post("/account/logout");
        Description(x => x.WithName("Logout"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies["REFRESH_TOKEN"];
        await _accountService.Logout(refreshToken);
        await Send.OkAsync(true, ct);
    }
}
