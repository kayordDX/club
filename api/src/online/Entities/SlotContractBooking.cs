namespace Online.Entities;

public class SlotContractBooking
{
    public int Id { get; set; }
    public int SlotContractId { get; set; }
    public SlotContract SlotContract { get; set; } = default!;
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = default!;
    public Guid? UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Cellphone { get; set; }
}
