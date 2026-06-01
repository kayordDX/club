using Club.Common.Enums;

namespace Club.Features.Booking.UpdateStatus;

public class BookingUpdateStatusRequest
{
    public int BookingId { get; set; }
    public BookingStatusEnum Status { get; set; }
}
