namespace Club.Features.Admin.Booking.AcceptPayment;

public class AdminBookingAcceptPaymentRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public decimal Amount { get; set; }
}
