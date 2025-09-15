
using Microsoft.AspNetCore.Identity.Data;
using Online.Services;

namespace Online.Features.Account.Login;

public class Endpoint : Endpoint<LoginRequest>
{
    private readonly AccountService accountService;

    public Endpoint(AccountService accountService)
    {
        this.accountService = accountService;
    }

    public override void Configure()
    {
        Post("/account/login");
        AllowAnonymous();
        Description(x => x.WithName("Login"));
    }

    public override async Task HandleAsync(LoginRequest r, CancellationToken ct)
    {
        await accountService.LoginAsync(r);
        await Send.OkAsync();
    }
}