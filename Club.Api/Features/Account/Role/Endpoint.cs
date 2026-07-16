using Club.Common;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Account.Role;

public class Endpoint(AppDbContext dbContext) : Endpoint<AccountRoleRequest, List<UserRoleBasicDTO>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/account/role/{facilityId}");
        Description(x => x.WithName("AccountRole"));
    }

    public override async Task HandleAsync(AccountRoleRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var roles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && ur.FacilityId == req.FacilityId)
            .Select(x => new UserRoleBasicDTO() { FacilityId = x.FacilityId, NormalizedName = x.Role.NormalizedName })
            .ToListAsync(ct);

        await Send.OkAsync(roles, ct);
    }
}
