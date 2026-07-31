using FluentValidation;

namespace Club.Features.Payment.Initiate;

public class PaymentInitiateValidator : AbstractValidator<PaymentInitiateRequest>
{
    public PaymentInitiateValidator()
    {
        RuleFor(x => x.BookingId).GreaterThan(0).WithMessage("Booking ID is required.");

        RuleFor(x => x.ProviderName).NotEmpty().WithMessage("Provider name is required.");
    }
}
