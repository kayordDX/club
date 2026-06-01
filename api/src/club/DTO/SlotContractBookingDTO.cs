namespace Online.DTO;

public class SlotContractBookingDTO
{
    public int Id { get; set; }
    public int SlotContractId { get; set; }
    public SlotContractDTO SlotContract { get; set; } = default!;
    public int BookingId { get; set; }
    public Guid? UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Cellphone { get; set; }
}
