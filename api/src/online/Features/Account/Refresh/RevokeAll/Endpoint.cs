using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Services;

namespace Online.Features.Account.Refresh.RevokeAll;

public class Endpoint(AppDbContext dbContext, CurrentUserService cu) : EndpointWithoutRequest
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly CurrentUserService _cu = cu;

    public override void Configure()
    {
        Post("/account/refresh/revokeAll");
        AllowAnonymous();
        Description(x => x.WithName("RefreshRevokeAll"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies["REFRESH_TOKEN"];
        var otherTokens = await _dbContext.UserRefreshToken
            .Where(x => x.UserId == _cu.GetId() && x.Token != refreshToken)
            .ToListAsync(ct);

        if (otherTokens == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        _dbContext.UserRefreshToken.RemoveRange(otherTokens);
        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
