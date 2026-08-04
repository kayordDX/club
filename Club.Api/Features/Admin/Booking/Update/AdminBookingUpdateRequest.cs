using Club.Features.Booking.Create;

namespace Club.Features.Admin.Booking.Update;

public class AdminBookingUpdateRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public List<BookingRequest> Bookings { get; set; } = [];
    public List<BookingExtraRequest> Extras { get; set; } = [];
}
