using Club.Common;
using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.GetPath;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingGetPathRequest, BookingPathDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/booking/{Id}/path");
        Description(x => x.WithName("BookingGetPath"));
    }

    public override async Task HandleAsync(BookingGetPathRequest req, CancellationToken ct)
    {
        if (Helpers.GetCurrentUserId(HttpContext) == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var path = await _dbContext
            .Booking.Where(b => b.Id == req.Id && b.FacilityId != null)
            .Select(b => new BookingPathDTO
            {
                BookingId = b.Id,
                OutletId = b.Facility!.OutletId,
                OutletSlug = b.Facility.Outlet.Slug,
                OutletName = b.Facility.Outlet.Name,
                FacilityId = b.Facility.Id,
                FacilityName = b.Facility.Name,
                SlotId = b.SlotContractBookings.OrderBy(scb => scb.Id).Select(scb => (Guid?)scb.SlotContract.SlotId).FirstOrDefault(),
                SlotStartDatetime = b
                    .SlotContractBookings.OrderBy(scb => scb.Id)
                    .Select(scb => (DateTime?)scb.SlotContract.Slot.StartDatetime)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        if (path == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(path, ct);
    }
}
