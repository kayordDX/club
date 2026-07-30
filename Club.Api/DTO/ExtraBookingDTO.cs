using Club.Entities;

namespace Club.DTO;

public class ExtraBookingDTO
{
    public int ExtraId { get; set; }
    public Extra Extra { get; set; } = default!;
    public int BookingId { get; set; }
    public int Amount { get; set; }
}
