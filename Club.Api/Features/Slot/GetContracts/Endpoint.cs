using Club.Common;
using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Slot.GetContracts;

public class Endpoint(AppDbContext dbContext) : Endpoint<SlotGetContractsRequest, List<SlotGetContractsResponse>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/slot/contracts/{Id}");
        Description(x => x.WithName("SlotGetContracts"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(SlotGetContractsRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        var facilityId = await _dbContext.Slot.Where(s => s.Id == req.Id).Select(s => (int?)s.FacilityId).FirstOrDefaultAsync(ct);
        var isManager =
            userId.HasValue
            && facilityId.HasValue
            && await _dbContext.UserRoles.AnyAsync(
                ur => ur.UserId == userId.Value && ur.FacilityId == facilityId && ur.Role.NormalizedName == Constants.Policy.Manager.ToUpperInvariant(),
                ct
            );

        var slotContracts = await _dbContext
            .SlotContract.Where(sc => sc.SlotId == req.Id)
            // Managers may book any contract for their facility. Anonymous and guest users
            // only see public contracts; logged-in users also see valid contracts they hold.
            .Where(sc =>
                isManager
                || sc.Contract.IsPublic
                || (
                    userId != null
                    && _dbContext.UserContract.Any(uc =>
                        uc.UserId == userId
                        && uc.ContractId == sc.ContractId
                        && uc.IsActive
                        && uc.StartDate.Date <= sc.Slot.StartDatetime.Date
                        && (uc.EndDate == null || sc.Slot.StartDatetime.Date <= uc.EndDate.Value.Date)
                    )
                )
            )
            .Select(sc => new SlotGetContractsResponse
            {
                Id = sc.Id,
                SlotId = sc.SlotId,
                ContractId = sc.ContractId,
                ContractName = sc.Contract.Name,
                Price = sc.Price,
                ValidationId = sc.ValidationId,
                CanPayLater = sc.CanPayLater,
                Description = sc.Description,
            })
            .OrderBy(sc => sc.Price)
            .ToListAsync(ct);

        await Send.OkAsync(slotContracts, ct);
    }
}
