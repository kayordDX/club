using Club.Common;
using Club.Common.Payments;
using Club.DTO;
using Club.Services;

namespace Club.Features.Payment.Vouchers;

public class PaymentVouchersRequest
{
    public int BookingId { get; set; }
}

public class Endpoint(BookingVoucherService vouchers, Club.Data.AppDbContext db) : Endpoint<PaymentVouchersRequest, List<BookingVoucherDTO>>
{
    public override void Configure()
    {
        Get("/payment/booking/{BookingId}/vouchers");
        Description(x => x.WithName("PaymentVouchers"));
    }

    public override async Task HandleAsync(PaymentVouchersRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var booking = await vouchers.GetBookingAsync(req.BookingId, userId.Value, ct);
        if (booking is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var available = await BookingPayments.AvailableAsync(db, booking, ct);
        var grants = await vouchers.GetGrantsAsync(userId.Value, ct);
        var results = new List<BookingVoucherDTO>();
        foreach (var grant in grants)
            results.Add(await vouchers.DescribeAsync(booking, grant, available, ct));
        await Send.OkAsync(results, ct);
    }
}
