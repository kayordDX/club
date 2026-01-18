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
        _accountService.Logout();
        await Send.OkAsync(true, ct);
    }
}
