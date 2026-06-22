using FluentValidation;

namespace Club.Features.Payment.Checkout;

public class PaymentCheckoutValidator : AbstractValidator<PaymentCheckoutRequest>
{
    public PaymentCheckoutValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider name is required.");

        RuleFor(x => x.TransactionId)
            .NotEmpty().WithMessage("Transaction ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.")
            .Must(c => c.All(char.IsLetter)).WithMessage("Currency must contain only letters.");
    }
}
