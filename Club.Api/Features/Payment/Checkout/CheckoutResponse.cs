namespace Club.Features.Payment.Checkout;

public class PaymentCheckoutResponse
{
    public bool Success { get; set; }
    public required string TransactionId { get; set; }
    public string? ProviderReference { get; set; }
    public string? RedirectUrl { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
