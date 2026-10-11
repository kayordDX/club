using Club.Common.Enums;

namespace Club.Entities;

public class BookingAction
{
    public long Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    // Retained even if the acting administrator's account is removed.
    public Guid ActingAdminId { get; set; }
    public BookingActionKind Kind { get; set; }
    public DateTime Timestamp { get; set; }
}
