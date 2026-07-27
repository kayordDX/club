namespace Club.Entities;

public class PaymentLog
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = default!;
    public required string TransactionId { get; set; }
    public required string ProviderName { get; set; }
    public required string EventType { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
