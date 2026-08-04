using Club.Common.Enums;

namespace Club.Features.Admin.Booking.UpdateStatus;

public class AdminBookingUpdateStatusRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public BookingStatusEnum Status { get; set; }
}
