using Club.Features.Payment.Initiate;

namespace Club.Features.Payment.Processing;

public class PaymentCheckoutRequest
{
    public required string ProviderName { get; set; }
    public decimal? Amount { get; set; }
    public PaymentInitiateRecurring? Recurring { get; set; }
}
