using Online.Data;
using Online.Models;
using Online.Services;

namespace Online.Features.Account.Refresh;

public class Endpoint(AccountService accountService, AppDbContext dbContext) : EndpointWithoutRequest<UserModel>
{
    private readonly AccountService accountService = accountService;
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/account/refresh");
        AllowAnonymous();
        Description(x => x.WithName("Refresh"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var refreshToken = HttpContext.Request.Cookies["REFRESH_TOKEN"];
            var userId = await accountService.RefreshTokenAsync(refreshToken, ct);

            // var userId = Helpers.GetCurrentUserId(HttpContext);
            var user = await Me.Data.Get(userId, _dbContext, ct);
            if (user == null)
            {
                ValidationContext.Instance.ThrowError("user_not_found");
            }
            await Send.OkAsync(user, ct);
        }
        catch (Exception)
        {
            accountService.Logout();
            await Send.ForbiddenAsync(ct);
            return;
        }
    }
}
