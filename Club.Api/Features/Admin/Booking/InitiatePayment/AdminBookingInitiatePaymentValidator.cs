using Club.Features.Payment.Processing;
using FluentValidation;

namespace Club.Features.Admin.Booking.InitiatePayment;

public class AdminBookingInitiatePaymentValidator : PaymentCheckoutValidator<AdminBookingInitiatePaymentRequest>
{
    public AdminBookingInitiatePaymentValidator()
    {
        RuleFor(x => x.FacilityId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
