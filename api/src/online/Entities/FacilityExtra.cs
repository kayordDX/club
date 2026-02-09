namespace Online.Entities;

public class FacilityExtra : AuditableEntity
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
    public int ExtraId { get; set; }
    public required Extra Extra { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public decimal Price { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int BookingStatusId { get; set; }
    public required BookingStatus BookingStatus { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsOnline { get; set; }
    public Guid UserId { get; set; }
    public required User User { get; set; }

    // Navigation properties
    public ICollection<ExtraBooking> ExtraBookings { get; set; } = new List<ExtraBooking>();
}
