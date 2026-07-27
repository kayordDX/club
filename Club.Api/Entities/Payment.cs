namespace Club.Entities;

public class Payment : AuditableEntity
{
    public int Id { get; set; }
    public int PaymentStatusId { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public required DateTime PaymentStatusDate { get; set; }
    public decimal Amount { get; set; }
    public int PaymentTypeId { get; set; }
    public PaymentType PaymentType { get; set; } = default!;
    public required string TransactionId { get; set; }
    public string? ProviderReference { get; set; }
    public required string ProviderName { get; set; }
    public string? RedirectUrl { get; set; }
    public string? FormActionUrl { get; set; }
    public string? FormFieldsJson { get; set; }
    public string? ErrorMessage { get; set; }
}
