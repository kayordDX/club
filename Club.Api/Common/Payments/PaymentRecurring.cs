using Club.Common.Enums;

namespace Club.Common.Payments;

// Optional recurring/subscription intent carried on a PaymentRequest. When present, a provider that
// supports recurring billing sets up a subscription instead of a once-off charge; providers that do
// not support it ignore this and fall back to a single payment.
public class PaymentRecurring
{
    public PaymentFrequencyEnum Frequency { get; set; }

    // Number of billing cycles to run. 0 means indefinite (until cancelled).
    public int Cycles { get; set; }

    // Amount charged on each recurring cycle. When null, the provider uses the request Amount.
    public decimal? RecurringAmount { get; set; }

    // Date of the first recurring charge. When null, the provider uses the current date.
    public DateOnly? FirstBillingDate { get; set; }
}
