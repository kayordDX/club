using Club.Features.Payment.Processing;
using FluentValidation;

namespace Club.Features.Payment.Initiate;

public class PaymentInitiateValidator : PaymentCheckoutValidator<PaymentInitiateRequest>
{
    public PaymentInitiateValidator()
    {
        RuleFor(x => x.BookingId).GreaterThan(0).WithMessage("Booking ID is required.");
    }
}
