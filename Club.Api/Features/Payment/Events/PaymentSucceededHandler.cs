using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Payment.Events;

public class PaymentSucceededHandler(AppDbContext dbContext) : IEventHandler<PaymentSucceededEvent>
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task HandleAsync(PaymentSucceededEvent eventModel, CancellationToken ct)
    {
        var booking = await _dbContext.Booking.FirstOrDefaultAsync(b => b.Id == eventModel.BookingId, ct);

        if (booking is null)
        {
            return;
        }

        booking.AmountPaid += eventModel.Amount;
        booking.AmountOutstanding -= eventModel.Amount;

        if (booking.AmountOutstanding <= 0)
        {
            booking.IsPaid = true;
            booking.BookingStatusId = (int)Common.Enums.BookingStatusEnum.Confirmed;
            booking.BookingStatusDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
