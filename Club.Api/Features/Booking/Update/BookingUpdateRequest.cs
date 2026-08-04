using Club.Features.Booking.Create;

namespace Club.Features.Booking.Update;

public class BookingUpdateRequest
{
    public int Id { get; set; }
    public List<BookingRequest> Bookings { get; set; } = [];
    public List<BookingExtraRequest> Extras { get; set; } = [];
}
