namespace Online.Entities;

public class Slot : AuditableEntity
{
    public Guid Id { get; set; }
    public int? ResourceId { get; set; }
    public Resource? Resource { get; set; }
    public required DateTime StartDatetime { get; set; }
    public required DateTime EndDatetime { get; set; }
    public int SlotGroupId { get; set; }
    public ICollection<SlotBooking> SlotBookings { get; set; } = [];
    public ICollection<SlotContract> SlotContracts { get; set; } = [];
    public ICollection<ExtraBooking> ExtraBookings { get; set; } = [];
}
