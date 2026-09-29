using Club.Common;
using Club.Common.Payments;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

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
        var booking = await db.Booking.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.BookingId && x.UserId == Helpers.GetCurrentUserId(HttpContext), ct);
        if (booking is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var payments = await db
            .PaymentBooking.Where(x => x.BookingId == booking.Id)
            .OrderBy(x => x.PaymentId)
            .Select(x => new PaymentDTO
            {
                Id = x.PaymentId,
                TransactionId = x.Payment.TransactionId,
                Amount = x.Payment.Amount,
                PaymentTypeId = x.Payment.PaymentTypeId,
                PaymentType = x.Payment.PaymentType.Name,
                PaymentStatusId = x.Payment.PaymentStatusId,
                PaymentStatus = x.Payment.PaymentStatus.Name,
                ProviderName = x.Payment.ProviderName,
                PaymentStatusDate = x.Payment.PaymentStatusDate,
            })
            .ToListAsync(ct);
        await Send.OkAsync(
            new BookingPaymentDTO
            {
                BookingId = booking.Id,
                AmountPaid = booking.AmountPaid,
                AmountOutstanding = booking.AmountOutstanding,
                AmountAvailable = await BookingPayments.AvailableAsync(db, booking, ct),
                IsPaid = booking.IsPaid,
                Payments = payments,
            },
            ct
        );
    }
}
