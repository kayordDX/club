using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Booking.Get;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminBookingGetRequest, BookingDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/booking/{Id}");
        Description(x => x.WithName("AdminBookingGet"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingGetRequest req, CancellationToken ct)
    {
        var result = await _dbContext
            .Booking.Where(b => b.Id == req.Id && b.SlotContractBookings.Any(scb => scb.SlotContract.Slot.FacilityId == req.FacilityId))
            .AsSplitQuery()
            .ProjectToDto()
            .FirstOrDefaultAsync(ct);

        if (result == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (result.BookingStatusId == (int)BookingStatusEnum.Pending && result.ExpiresAt < DateTime.UtcNow)
        {
            result.BookingStatusId = (int)BookingStatusEnum.Expired;
            if (result.BookingStatus != null)
            {
                result.BookingStatus.Id = (int)BookingStatusEnum.Expired;
                result.BookingStatus.Name = "Expired";
            }
        }

        await Send.OkAsync(result, ct);
    }
}
