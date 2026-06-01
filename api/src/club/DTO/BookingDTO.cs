namespace Online.DTO;

public class BookingDTO
{
    public int Id { get; set; }
    public int BookingStatusId { get; set; }
    public BookingStatusDTO BookingStatus { get; set; } = default!;
    public required DateTime BookingStatusDate { get; set; }
    public Guid? UserId { get; set; }
    public UserDTO? User { get; set; }
    public bool IsPaid { get; set; }
    public decimal AmountOutstanding { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<SlotContractBookingDTO> SlotContractBookings { get; set; } = [];
}
