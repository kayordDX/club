namespace Club.Features.Booking.GetPath;

public class BookingPathDTO
{
    public int BookingId { get; set; }
    public int OutletId { get; set; }
    public string OutletSlug { get; set; } = "";
    public string OutletName { get; set; } = "";
    public int FacilityId { get; set; }
    public string FacilityName { get; set; } = "";
    public Guid SlotId { get; set; }
    public DateTime SlotStartDatetime { get; set; }
}
