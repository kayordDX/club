namespace Online.Entities;

public class ExtraBooking
{
    public int ExtraId { get; set; }
    public Extra Extra { get; set; } = default!;
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;
}
