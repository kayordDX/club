namespace Club.Features.Payment.Initiate;

public class PaymentInitiateResponse
{
    public required string TransactionId { get; set; }
    public required string RedirectUrl { get; set; }
    public string? ProviderReference { get; set; }
}
