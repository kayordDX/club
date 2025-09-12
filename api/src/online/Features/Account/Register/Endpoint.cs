
using Online.Services;

namespace Online.Features.Account.Register;

public class Endpoint : Endpoint<UserRegisterRequest>
{
    private readonly AccountService accountService;

    public Endpoint(AccountService accountService)
    {
        this.accountService = accountService;
    }

    public override void Configure()
    {
        Get("/account/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UserRegisterRequest r, CancellationToken ct)
    {
        await accountService.RegisterAsync(r);
        await Send.CreatedAtAsync("user");
    }
}