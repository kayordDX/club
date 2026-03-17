namespace Online.Entities;

public class Payment : AuditableEntity
{
    public int Id { get; set; }
    public int PaymentStatusId { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public required DateTime PaymentStatusDate { get; set; }
    public decimal Amount { get; set; }
    public int PaymentTypeId { get; set; }
    public PaymentType PaymentType { get; set; } = default!;
}
