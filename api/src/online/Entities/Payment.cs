namespace Online.Entities;

public class Payment
{
    public int Id { get; set; }
    public int PaymentStatusId { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public required DateTime PaymentStatusDate { get; set; }
    public decimal Amount { get; set; }

    // Navigation properties
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<SlotBooking> SlotBookings { get; set; } = new List<SlotBooking>();
    public ICollection<ExtraBooking> ExtraBookings { get; set; } = new List<ExtraBooking>();
}
