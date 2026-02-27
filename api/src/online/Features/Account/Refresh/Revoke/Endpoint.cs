using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.Account.Refresh.Revoke;

public class Endpoint(AppDbContext dbContext) : Endpoint<RefreshRevokeRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/account/refresh/revoke");
        AllowAnonymous();
        Description(x => x.WithName("RefreshRevoke"));
    }

    public override async Task HandleAsync(RefreshRevokeRequest r, CancellationToken ct)
    {
        var token = await _dbContext.UserRefreshToken.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
        if (token == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        _dbContext.UserRefreshToken.Remove(token);
        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
