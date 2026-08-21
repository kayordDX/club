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

        // Guard against bookings without slot contract bookings so the First() projection
        // below returns 404 instead of crashing with a 500.
        var path = await _dbContext
            .Booking.Where(b => b.Id == req.Id && b.SlotContractBookings.Any())
            .Select(b => new BookingPathDTO
            {
                BookingId = b.Id,
                OutletId = b.SlotContractBookings.First().SlotContract.Slot.Facility!.OutletId,
                OutletSlug = b.SlotContractBookings.First().SlotContract.Slot.Facility!.Outlet.Slug,
                OutletName = b.SlotContractBookings.First().SlotContract.Slot.Facility!.Outlet.Name,
                FacilityId = b.SlotContractBookings.First().SlotContract.Slot.Facility!.Id,
                FacilityName = b.SlotContractBookings.First().SlotContract.Slot.Facility!.Name,
                SlotId = b.SlotContractBookings.First().SlotContract.Slot.Id,
                SlotStartDatetime = b.SlotContractBookings.First().SlotContract.Slot.StartDatetime,
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
