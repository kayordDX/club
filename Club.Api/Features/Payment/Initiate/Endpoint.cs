using Club.Common;
using Club.Data;
using Club.Features.Payment.Processing;
using Club.Services;

namespace Club.Features.Payment.Initiate;

public class Endpoint(AppDbContext db, IPaymentFactory factory, PaymentLogger paymentLogger, ILogger<Endpoint> logger)
    : CheckoutEndpoint<PaymentInitiateRequest>(db, factory, paymentLogger, logger)
{
    public override void Configure()
    {
        Post("/payment/initiate");
        Description(x => x.WithName("PaymentInitiate"));
    }

    protected override int GetBookingId(PaymentInitiateRequest req) => req.BookingId;

    protected override bool CanAccess(Entities.Booking booking, PaymentInitiateRequest req) =>
        Helpers.GetCurrentUserId(HttpContext) is { } userId && booking.UserId == userId;
}
