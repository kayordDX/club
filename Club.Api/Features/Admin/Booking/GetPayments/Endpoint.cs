using Club.Common.Payments;
using Club.Data;
using Club.DTO;
using Club.Features.Payment.Processing;

namespace Club.Features.Admin.Booking.GetPayments;

public class Endpoint(AppDbContext db) : Endpoint<AdminBookingGetPaymentsRequest, BookingPaymentDTO>
{
    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/booking/{Id}/payments");
        Description(x => x.WithName("AdminBookingGetPayments"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingGetPaymentsRequest req, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var booking = await BookingPayments.LockAsync(db, req.Id, ct);
        if (booking is null || booking.FacilityId != req.FacilityId)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var result = await BookingPaymentHistory.GetAsync(db, booking, ct);
        await transaction.CommitAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
