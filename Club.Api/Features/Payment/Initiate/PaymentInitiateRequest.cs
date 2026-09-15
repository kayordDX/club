using Club.Common.Enums;

namespace Club.Features.Payment.Initiate;

public class PaymentInitiateRequest
{
    public int BookingId { get; set; }
    public required string ProviderName { get; set; }

    // Optional recurring/subscription intent. When null the payment is a once-off charge (existing
    // behaviour). When set, the selected provider sets up a recurring subscription if it supports it.
    public PaymentInitiateRecurring? Recurring { get; set; }
}

public class PaymentInitiateRecurring
{
    public PaymentFrequencyEnum Frequency { get; set; }

    // Number of billing cycles. 0 means indefinite (until cancelled).
    public int Cycles { get; set; }

    // Amount charged on each cycle. When null the booking amount is used.
    public decimal? RecurringAmount { get; set; }

    // First recurring charge date. When null the provider uses the current date.
    public DateOnly? FirstBillingDate { get; set; }
}
