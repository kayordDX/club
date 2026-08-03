using Club.Common.Enums;
using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Slot.Available;

public class Endpoint(AppDbContext dbContext) : Endpoint<AvailableSlotRequest, bool>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/slot/available");
        Description(x => x.WithName("SlotAvailable"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(AvailableSlotRequest r, CancellationToken ct)
    {
        var slot = await _dbContext.Slot.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
        if (slot == null)
        {
            ValidationContext.Instance.ThrowError("Slot not found");
        }

        int bookedCount = await _dbContext
            .SlotContractBooking.Where(x =>
                x.SlotContract.SlotId == r.Id
                && x.Booking.BookingStatusId != (int)BookingStatusEnum.Cancelled
                && (x.Booking.BookingStatusId != (int)BookingStatusEnum.Pending || x.Booking.ExpiresAt >= DateTime.UtcNow)
            )
            .CountAsync(ct);
        bool result = (slot.MaxBookings - bookedCount) >= (r.SlotCount ?? 1);
        await Send.OkAsync(result, ct);
    }
}
