using Online.Data;
using Online.Services;
using Microsoft.EntityFrameworkCore;

namespace Online.Features.Account.Refresh.List;

public class Endpoint(AppDbContext dbContext, CurrentUserService currentUserService) : EndpointWithoutRequest<List<RefreshListResponse>>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly CurrentUserService _currentUserService = currentUserService;

    public override void Configure()
    {
        Get("/account/refresh/list");
        AllowAnonymous();
        Description(x => x.WithName("RefreshList"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var refreshToken = HttpContext.Request.Cookies["REFRESH_TOKEN"];
        var refreshTokens = await _dbContext.UserRefreshToken
            .Where(x => x.UserId == _currentUserService.GetId())
            .Select(x => new RefreshListResponse
            {
                Id = x.Id,
                ExpiresAtUtc = x.ExpiresAtUtc,
                Browser = x.Browser,
                BrowserVersion = x.BrowserVersion,
                Device = x.Device,
                Platform = x.Platform,
                IsCurrent = refreshToken == x.Token
            }).ToListAsync(ct);
        await Send.OkAsync(refreshTokens, ct);
    }
}
