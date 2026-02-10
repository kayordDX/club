namespace Online.Entities;

public class Payment
{
    public int Id { get; set; }
    public int PaymentStatusId { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public required DateTime PaymentStatusDate { get; set; }
    public decimal Amount { get; set; }
}
