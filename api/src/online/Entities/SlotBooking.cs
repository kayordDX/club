namespace Online.Entities;

public class SlotBooking : AuditableEntity
{
    public int Id { get; set; }
    public int SlotContractId { get; set; }
    public required SlotContract SlotContract { get; set; }
    public int BookingStatusId { get; set; }
    public required BookingStatus BookingStatus { get; set; }
    public required DateTime BookingStatusDate { get; set; }
    public Guid UserId { get; set; }
    public required User User { get; set; }
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
