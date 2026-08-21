using Club.Common;
using Club.Common.Enums;
using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.UpdateStatus;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingUpdateStatusRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Put("/booking/status");
        Description(x => x.WithName("BookingUpdateStatus"));
    }

    public override async Task HandleAsync(BookingUpdateStatusRequest req, CancellationToken ct)
    {
        if (Helpers.GetCurrentUserId(HttpContext) == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Validate if booking exists
        var booking = await _dbContext.Booking.FirstOrDefaultAsync(b => b.Id == req.BookingId, ct);

        if (booking is null)
        {
            AddError(r => r.BookingId, "Booking not found.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        // Validate if you can update that status
        if (req.Status != BookingStatusEnum.Cancelled && req.Status != BookingStatusEnum.Confirmed)
        {
            AddError(r => r.Status, "Invalid status");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        // Cancelling releases the slot via the availability queries (they exclude cancelled
        // bookings), but the slot contract bookings are kept so the booking keeps its history
        // (facility, times, players) and the status change stays reversible — same as the admin endpoint.
        booking.BookingStatusId = (int)req.Status;
        booking.BookingStatusDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
