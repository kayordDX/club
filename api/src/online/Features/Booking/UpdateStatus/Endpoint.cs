using Microsoft.EntityFrameworkCore;
using Online.Common.Enums;
using Online.Data;

namespace Online.Features.Booking.UpdateStatus;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingUpdateStatusRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Put("/booking/status");
        Description(x => x.WithName("BookingUpdateStatus"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(BookingUpdateStatusRequest req, CancellationToken ct)
    {
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

        if (req.Status == BookingStatusEnum.Cancelled)
        {
            await _dbContext.SlotContractBooking.Where(x => x.BookingId == booking.Id).ExecuteDeleteAsync(ct);
        }

        // Update status
        booking.BookingStatusId = (int)req.Status;
        await _dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
