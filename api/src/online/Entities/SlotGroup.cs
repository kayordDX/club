namespace Online.Entities;

public class SlotGroup : AuditableEntity
{
    public int Id { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }

    // Navigation properties
    public ICollection<Slot> Slots { get; set; } = [];
}
