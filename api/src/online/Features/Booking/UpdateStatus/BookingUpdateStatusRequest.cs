using Online.Common.Enums;

namespace Online.Features.Booking.UpdateStatus;

public class BookingUpdateStatusRequest
{
    public int BookingId { get; set; }
    public BookingStatusEnum Status { get; set; }
}
