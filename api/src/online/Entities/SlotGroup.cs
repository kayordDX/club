namespace Online.Entities;

public class SlotGroup
{
    public Guid Id { get; set; }
    public bool CanBookForGuests { get; set; }
    public int? ResourceId { get; set; }
    public Resource? Resource { get; set; }
    public int? FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public ICollection<Slot> Slots { get; set; } = [];
}
