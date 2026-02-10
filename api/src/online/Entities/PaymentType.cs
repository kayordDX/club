namespace Online.Entities;

public class PaymentType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Bill> Bills { get; set; } = [];
}
