namespace Online.Entities;

public class Resource : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Slot> Slots { get; set; } = new List<Slot>();
}
