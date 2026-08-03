using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Payment.Events;

public class PaymentSucceededHandler(AppDbContext dbContext) : IEventHandler<PaymentSucceededEvent>
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task HandleAsync(PaymentSucceededEvent eventModel, CancellationToken ct)
    {
        // Booking amounts/status are applied synchronously in
        // PaymentResultHandler.PersistAndUpdateBookingAsync before this event is published.
        // This handler must NOT re-apply them — doing so double-credits AmountPaid/
        // AmountOutstanding for every successful payment. The event exists for side effects
        // (e.g. future notifications/audit).
        var booking = await _dbContext.Booking.FirstOrDefaultAsync(b => b.Id == eventModel.BookingId, ct);

        if (booking is null)
        {
            return;
        }
    }
}
