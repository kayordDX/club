using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Booking.UpdateStatus;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminBookingUpdateStatusRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Put("/admin/facility/{FacilityId}/booking/{Id}/status");
        Description(x => x.WithName("AdminBookingUpdateStatus"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingUpdateStatusRequest req, CancellationToken ct)
    {
        if (!Enum.IsDefined(req.Status))
        {
            AddError(r => r.Status, "Invalid status.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var booking = await _dbContext
            .Booking.Where(b => b.Id == req.Id && b.SlotContractBookings.Any(scb => scb.SlotContract.Slot.FacilityId == req.FacilityId))
            .FirstOrDefaultAsync(ct);

        if (booking is null)
        {
            AddError(r => r.Id, "Booking not found.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        // Managers can freely change status. We intentionally do NOT remove slot contract
        // bookings here (unlike the user-facing cancel) so status changes stay reversible.
        booking.BookingStatusId = (int)req.Status;
        booking.BookingStatusDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
