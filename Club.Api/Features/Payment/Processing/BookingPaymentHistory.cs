using Club.Common.Payments;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Payment.Processing;

internal static class BookingPaymentHistory
{
    // Read while holding the booking lock so balances and attempts represent one consistent state.
    public static async Task<BookingPaymentDTO> GetAsync(AppDbContext db, Entities.Booking booking, CancellationToken ct)
    {
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
        return new BookingPaymentDTO
        {
            BookingId = booking.Id,
            AmountPaid = booking.AmountPaid,
            AmountOutstanding = booking.AmountOutstanding,
            AmountAvailable = await BookingPayments.AvailableAsync(db, booking, ct),
            IsPaid = booking.IsPaid,
            Payments = payments,
        };
    }
}
