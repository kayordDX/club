using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Common.Payments;

public static class BookingPayments
{
    public static bool IsSettled(int statusId) => statusId == (int)PaymentStatusEnum.Completed || statusId == (int)PaymentStatusEnum.Partial;

    public static bool IsValidAmount(decimal amount, decimal available) => amount > 0 && amount <= available && decimal.Round(amount, 2) == amount;

    // Call inside a transaction. All booking payment writers acquire this lock first.
    public static Task<Booking?> LockAsync(AppDbContext db, int bookingId, CancellationToken ct) =>
        db.Booking.FromSqlInterpolated($"SELECT * FROM booking WHERE id = {bookingId} FOR UPDATE").FirstOrDefaultAsync(ct);

    public static Task<decimal> AvailableAsync(AppDbContext db, Booking booking, CancellationToken ct) =>
        Task.FromResult(Math.Max(0, booking.AmountOutstanding));

    public static async Task ApplyAsync(AppDbContext db, Booking booking, Payment payment, CancellationToken ct)
    {
        if (!IsValidAmount(payment.Amount, booking.AmountOutstanding))
            throw new InvalidOperationException("Payment exceeds the outstanding booking balance or has an invalid amount.");

        booking.AmountPaid += payment.Amount;
        booking.AmountOutstanding -= payment.Amount;
        booking.IsPaid = booking.AmountOutstanding == 0;
        payment.PaymentStatusId = (int)(booking.IsPaid ? PaymentStatusEnum.Completed : PaymentStatusEnum.Partial);
        payment.PaymentStatusDate = DateTime.UtcNow;

        if (booking.IsPaid)
        {
            booking.BookingStatusId = (int)BookingStatusEnum.Confirmed;
            booking.BookingStatusDate = DateTime.UtcNow;
            var partials = await db
                .PaymentBooking.Where(x => x.BookingId == booking.Id && x.Payment.PaymentStatusId == (int)PaymentStatusEnum.Partial)
                .Select(x => x.Payment)
                .ToListAsync(ct);
            foreach (var partial in partials)
            {
                partial.PaymentStatusId = (int)PaymentStatusEnum.Completed;
                partial.PaymentStatusDate = DateTime.UtcNow;
            }
        }
    }
}
