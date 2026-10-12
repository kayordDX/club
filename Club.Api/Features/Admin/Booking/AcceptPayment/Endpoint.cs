using Club.Common.Enums;
using Club.Common.Payments;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Club.Features.Booking.Common;
using Club.Features.Payment.Processing;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Booking.AcceptPayment;

public class Endpoint(AppDbContext db) : Endpoint<AdminBookingAcceptPaymentRequest, BookingPaymentDTO>
{
    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/booking/{Id}/payment/accept");
        Description(x => x.WithName("AdminBookingAcceptPayment"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingAcceptPaymentRequest req, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var booking = await BookingPayments.LockAsync(db, req.Id, ct);
        if (booking is null || booking.FacilityId != req.FacilityId)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (!ReservationRules.IsActive(booking, DateTime.UtcNow))
        {
            AddError("Restore inactive bookings before accepting payment.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var available = await BookingPayments.AvailableAsync(db, booking, ct);
        if (!BookingPayments.IsValidAmount(req.Amount, available))
        {
            AddError(x => x.Amount, "Payment must be positive, have at most two decimal places, and not exceed the available balance.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var payment = new Entities.Payment
        {
            Amount = req.Amount,
            PaymentStatusId = (int)PaymentStatusEnum.Completed,
            PaymentStatus = await db.PaymentStatus.FirstAsync(x => x.Id == (int)PaymentStatusEnum.Completed, ct),
            PaymentStatusDate = DateTime.UtcNow,
            PaymentTypeId = (int)PaymentTypeEnum.PayOnArrival,
            TransactionId = Guid.NewGuid().ToString(),
            ProviderName = "Admin received payment in the club",
        };
        db.Payment.Add(payment);
        db.PaymentBooking.Add(new PaymentBooking { Booking = booking, Payment = payment });
        await BookingPayments.ApplyAsync(db, booking, payment, ct);
        await db.SaveChangesAsync(ct);
        var result = await BookingPaymentHistory.GetAsync(db, booking, ct);
        await transaction.CommitAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
