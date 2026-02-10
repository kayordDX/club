namespace Online.Entities;

public class ExtraBooking
{
    public int Id { get; set; }
    public int FacilityExtraId { get; set; }
    public required FacilityExtra FacilityExtra { get; set; }
    public Guid SlotId { get; set; }
    public required Slot Slot { get; set; }
    public int BookingStatusId { get; set; }
    public required BookingStatus BookingStatus { get; set; }
    public required DateTime StatusDate { get; set; }
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid UserId { get; set; }
    public required User User { get; set; }
}
