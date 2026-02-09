namespace Online.Entities;

public class Validation
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<SlotContract> SlotContracts { get; set; } = new List<SlotContract>();
}
