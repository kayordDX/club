namespace Club.Features.Admin.Slot.GetAll;

public class AdminSlotGetAllResponse
{
    public Guid Id { get; set; }
    public string? ResourceName { get; set; }
    public DateTime StartDatetime { get; set; }
    public DateTime? EndDatetime { get; set; }
    public int Booked { get; set; }
    public int Total { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsAvailable => Booked < Total;
    public List<AdminSlotBookingDTO> Bookings { get; set; } = [];
}

public class AdminSlotBookingDTO
{
    public int BookingId { get; set; }
    public string? PlayerName { get; set; }
    public int BookingStatusId { get; set; }
    public string BookingStatusName { get; set; } = default!;
}
