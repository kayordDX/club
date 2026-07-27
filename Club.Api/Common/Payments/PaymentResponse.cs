namespace Club.Common.Payments;

public class PaymentResponse
{
    public bool Success { get; set; }
    public required string TransactionId { get; set; }
    public string? ProviderReference { get; set; }
    public string? RedirectUrl { get; set; }
    public string? FormActionUrl { get; set; }
    public Dictionary<string, string>? FormFields { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
