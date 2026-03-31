namespace Online.Entities;

public class Booking : AuditableEntity
{
    public int Id { get; set; }
    public int BookingStatusId { get; set; }
    public BookingStatus BookingStatus { get; set; } = default!;
    public required DateTime BookingStatusDate { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public bool IsPaid { get; set; }
    public decimal AmountOutstanding { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ICollection<SlotContractBooking> SlotContractBookings { get; set; } = [];
}
