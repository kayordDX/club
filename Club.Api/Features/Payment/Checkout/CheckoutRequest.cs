namespace Club.Features.Payment.Checkout;

public class PaymentCheckoutRequest
{
    public string Provider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public required string TransactionId { get; set; }
    public string? Description { get; set; }
}
