namespace Club.Common.Payments;

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public required string TransactionId { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
