namespace Club.Common.Payments;

public class PaymentResult
{
    public bool Success { get; set; }
    public required string TransactionId { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
