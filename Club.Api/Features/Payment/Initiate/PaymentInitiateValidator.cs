using FluentValidation;

namespace Club.Features.Payment.Initiate;

public class PaymentInitiateValidator : AbstractValidator<PaymentInitiateRequest>
{
    public PaymentInitiateValidator()
    {
        RuleFor(x => x.BookingId).GreaterThan(0).WithMessage("Booking ID is required.");

        RuleFor(x => x.ProviderName).NotEmpty().WithMessage("Provider name is required.");

        When(
            x => x.Recurring is not null,
            () =>
            {
                RuleFor(x => x.Recurring!.Frequency).IsInEnum().WithMessage("A valid recurring frequency is required.");

                RuleFor(x => x.Recurring!.Cycles).GreaterThanOrEqualTo(0).WithMessage("Cycles cannot be negative (0 means indefinite).");

                RuleFor(x => x.Recurring!.RecurringAmount)
                    .GreaterThan(0)
                    .When(x => x.Recurring!.RecurringAmount.HasValue)
                    .WithMessage("Recurring amount must be greater than zero.");

                // PayFast rejects a billing_date in the past at gateway time; catch it here as a clean
                // 400 instead. Day-granularity against UTC is intentional (SAST leads UTC by 2h, so the
                // guard is never tighter than a day).
                RuleFor(x => x.Recurring!.FirstBillingDate)
                    .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
                    .When(x => x.Recurring!.FirstBillingDate.HasValue)
                    .WithMessage("First billing date cannot be in the past.");
            }
        );
    }
}
