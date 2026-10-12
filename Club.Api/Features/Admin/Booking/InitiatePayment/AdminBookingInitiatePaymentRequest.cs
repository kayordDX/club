using Club.Features.Payment.Processing;

namespace Club.Features.Admin.Booking.InitiatePayment;

public class AdminBookingInitiatePaymentRequest : PaymentCheckoutRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
}
