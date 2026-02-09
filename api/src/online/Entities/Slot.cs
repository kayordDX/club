namespace Online.Entities;

public class Slot : AuditableEntity
{
    public int Id { get; set; }
    public int? ResourceId { get; set; }
    public Resource? Resource { get; set; }
    public required DateTime StartDatetime { get; set; }
    public required DateTime EndDatetime { get; set; }
    public int SlotGroupId { get; set; }
    public required SlotGroup SlotGroup { get; set; }

    // Navigation properties
    public ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();
    public ICollection<SlotContract> SlotContracts { get; set; } = new List<SlotContract>();
    public ICollection<ExtraBooking> ExtraBookings { get; set; } = new List<ExtraBooking>();
}
