namespace Online.Entities;

public class Booking : AuditableEntity
{
    public int Id { get; set; }
    public int BookingStatusId { get; set; }
    public required DateTime StatusDate { get; set; }
    public Guid UserId { get; set; }
    public required User User { get; set; }
    public required BookingStatus BookingStatus { get; set; }
    public ICollection<BookingSlot> BookingSlots { get; set; } = [];
}
