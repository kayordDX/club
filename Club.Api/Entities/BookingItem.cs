namespace Club.Entities;

public class BookingItem : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;
    public string? Description { get; set; }
}
