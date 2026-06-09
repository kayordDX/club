using Club.Common;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Account.Me;

public class Endpoint(AppDbContext dbContext) : EndpointWithoutRequest<List<UserRoleBasicDTO>>
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
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var roles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(x => new UserRoleBasicDTO() { FacilityId = x.FacilityId, NormalizedName = x.Role.NormalizedName })
            .ToListAsync(ct);

        await Send.OkAsync(roles, ct);
    }
}
