namespace Online.Entities;

public class PaymentStatus
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<SlotBooking> SlotBookings { get; set; } = new List<SlotBooking>();
    public ICollection<ExtraBooking> ExtraBookings { get; set; } = new List<ExtraBooking>();
    public ICollection<FacilityExtra> FacilityExtras { get; set; } = new List<FacilityExtra>();
}
