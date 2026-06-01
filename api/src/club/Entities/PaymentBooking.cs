namespace Online.Entities;

public class PaymentBooking
{
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = default!;
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;
}
