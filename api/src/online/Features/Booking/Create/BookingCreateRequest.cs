namespace Online.Features.Booking.Create;

public class BookingCreateRequest
{
    public List<BookingRequest> Bookings { get; set; } = [];
}

public class BookingRequest
{
    public Guid SlotId { get; set; }
    public int SlotContractId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Cellphone { get; set; }
}
