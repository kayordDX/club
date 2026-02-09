namespace Online.Entities;

public class BookingStatus
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<SlotBooking> SlotBookings { get; set; } = new List<SlotBooking>();
    public ICollection<ExtraBooking> ExtraBookings { get; set; } = new List<ExtraBooking>();
    public ICollection<FacilityExtra> FacilityExtras { get; set; } = new List<FacilityExtra>();
}
