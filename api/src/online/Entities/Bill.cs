namespace Online.Entities;

public class Bill : AuditableEntity
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public required Payment Payment { get; set; }
    public int PaymentTypeId { get; set; }
    public required PaymentType PaymentType { get; set; }
    public decimal Amount { get; set; }
    public required DateTime ReceivedDate { get; set; }
    public Guid ReceivedBy { get; set; }
    public required User User { get; set; }
}
