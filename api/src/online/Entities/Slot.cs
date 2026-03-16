namespace Online.Entities;

public class Slot : AuditableEntity
{
    public Guid Id { get; set; }
    public int? ResourceId { get; set; }
    public Resource? Resource { get; set; }
    public int? FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public required DateTime StartDatetime { get; set; }
    public DateTime? EndDatetime { get; set; }
    public Guid? SlotGroupId { get; set; }
    public bool CanPayLater { get; set; }
    public bool RequiresLogin { get; set; }
    public ICollection<SlotBooking> SlotBookings { get; set; } = [];
    public ICollection<SlotContract> SlotContracts { get; set; } = [];
    public ICollection<ExtraBooking> ExtraBookings { get; set; } = [];
}
