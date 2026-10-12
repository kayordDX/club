using Club.Data;
using Club.Features.Payment.Processing;
using Club.Services;

namespace Club.Features.Admin.Booking.InitiatePayment;

public class Endpoint(AppDbContext db, IPaymentFactory factory, PaymentLogger paymentLogger, ILogger<Endpoint> logger)
    : CheckoutEndpoint<AdminBookingInitiatePaymentRequest>(db, factory, paymentLogger, logger)
{
    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/booking/{Id}/payment/initiate");
        Description(x => x.WithName("AdminBookingInitiatePayment"));
        Policies(Constants.Policy.Manager);
    }

    protected override int GetBookingId(AdminBookingInitiatePaymentRequest req) => req.Id;

    protected override bool CanAccess(Entities.Booking booking, AdminBookingInitiatePaymentRequest req) => booking.FacilityId == req.FacilityId;
}
