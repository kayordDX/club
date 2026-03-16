namespace Online.Entities;

public class SlotBooking : AuditableEntity
{
    public int Id { get; set; }
    public int SlotContractId { get; set; }
    public SlotContract SlotContract { get; set; } = default!;
    public Guid? SlotId { get; set; }
    public Slot? Slot { get; set; }
    public int BookingStatusId { get; set; }
    public BookingStatus BookingStatus { get; set; } = default!;
    public required DateTime BookingStatusDate { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? Email { get; set; }
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
