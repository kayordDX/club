namespace Online.Features.Booking.Create;

public class BookingCreateResponse
{
    public List<int> BookingIds { get; set; } = [];
    public Guid SlotId { get; set; }
    public int Quantity { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
