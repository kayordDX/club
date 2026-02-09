namespace Online.Entities;

public class PaymentType
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}
