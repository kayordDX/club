namespace Club.Features.Admin.Booking.GetOptions;

public class AdminBookingGetOptionsRequest
{
    public int FacilityId { get; set; }
    public Guid SlotId { get; set; }
    public Guid? UserId { get; set; }
}
