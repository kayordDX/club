using Club.Data;
using Club.Features.Booking.Common;
using Club.Services;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Booking.GetOptions;

public class Endpoint(AppDbContext dbContext, ICustomKeycloakService keycloakService) : Endpoint<AdminBookingGetOptionsRequest, AdminBookingOptionsDTO>
{
    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/booking/options");
        Description(x => x.WithName("AdminBookingGetOptions"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingGetOptionsRequest req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var slot = await dbContext.Slot.FirstOrDefaultAsync(x => x.Id == req.SlotId && x.FacilityId == req.FacilityId, ct);
        if (slot is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var ownerExistsLocally = await dbContext.Users.AnyAsync(x => x.Id == req.UserId, ct);
        if (!ownerExistsLocally && await keycloakService.GetUserAsync(req.UserId, ct) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var ownerContracts = await dbContext.UserContract.Where(x => x.UserId == req.UserId && x.IsActive).ToListAsync(ct);
        var slotContracts = await dbContext.SlotContract.Where(x => x.SlotId == slot.Id).Include(x => x.Contract).ToListAsync(ct);
        var contracts = slotContracts
            .Where(x => ContractEligibility.IsAllowed(x.Contract, slot.StartDatetime, ownerContracts))
            .Select(x => new Club.Features.Slot.GetContracts.SlotGetContractsResponse
            {
                Id = x.Id,
                SlotId = x.SlotId,
                ContractId = x.ContractId,
                ContractName = x.Contract.Name,
                Price = x.Price,
                ValidationId = x.ValidationId,
                CanPayLater = x.CanPayLater,
                Description = x.Description,
            })
            .OrderBy(x => x.Price)
            .ToList();
        var extras = await dbContext
            .Extra.Where(x => x.FacilityId == req.FacilityId && x.IsAvailable && x.IsOnline)
            .Select(x => new AdminBookingExtraOptionDTO
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
            })
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        await Send.OkAsync(new AdminBookingOptionsDTO { Contracts = contracts, Extras = extras }, ct);
    }
}
