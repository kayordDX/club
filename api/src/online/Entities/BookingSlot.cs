namespace Online.Entities;

public class BookingSlot
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public required Booking Booking { get; set; }
    public int SlotId { get; set; }
    public required Slot Slot { get; set; }
}
