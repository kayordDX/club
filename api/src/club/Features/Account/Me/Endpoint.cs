using Online.Common;
using Online.Data;
using Online.Models;

namespace Online.Features.Account.Me;

public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<UserModel?>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/account/me");
        Description(x => x.WithName("AccountMe"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);

        var user = await Data.Get(userId, _dbContext, ct);

        await Send.OkAsync(user, ct);
    }
}
