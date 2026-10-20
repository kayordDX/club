using Club.Features.Booking.Create;

namespace Club.Features.Admin.Booking.Create;

public class AdminBookingCreateRequest : BookingCreateRequest
{
    public int FacilityId { get; set; }
    public Guid? UserId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public int? BookingStatusId { get; set; }
}
