
using Microsoft.AspNetCore.Identity.Data;
using Online.Services;

namespace Online.Features.Account.Login;

public class Endpoint(AccountService accountService) : Endpoint<LoginRequest, bool>
{
    private readonly AccountService accountService = accountService;

    public override void Configure()
    {
        Post("/account/login");
        AllowAnonymous();
        Description(x => x.WithName("Login"));
    }

    public override async Task HandleAsync(LoginRequest r, CancellationToken ct)
    {
        await accountService.LoginAsync(r);
        await Send.OkAsync(true, ct);
    }
}
