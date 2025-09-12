
using Online.Services;

namespace Online.Features.Account.Refresh;

public class Endpoint : EndpointWithoutRequest
{
    private readonly AccountService accountService;

    public Endpoint(AccountService accountService)
    {
        this.accountService = accountService;
    }

    public override void Configure()
    {
        Post("/account/refresh");
        AllowAnonymous();
        Description(x => x.WithName("Refresh"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies["REFRESH_TOKEN"];
        await accountService.RefreshTokenAsync(refreshToken);
        await Send.OkAsync();
    }
}