using Club.Common;
using Club.Common.Payments;
using Club.Data;
using Club.DTO;
using Club.Features.Payment.Processing;

namespace Club.Features.Payment.GetBooking;

public class PaymentGetBookingRequest
{
    public int BookingId { get; set; }
}

public class Endpoint(AppDbContext db) : Endpoint<PaymentGetBookingRequest, BookingPaymentDTO>
{
    public override void Configure()
    {
        Get("/payment/booking/{BookingId}");
        Description(x => x.WithName("PaymentGetBooking"));
    }

    public override async Task HandleAsync(PaymentGetBookingRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var booking = await BookingPayments.LockAsync(db, req.BookingId, ct);
        if (booking is null || booking.UserId != userId)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var result = await BookingPaymentHistory.GetAsync(db, booking, ct);
        await transaction.CommitAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
