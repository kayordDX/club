namespace Online.DTO;

public class SlotDTO
{
    public Guid Id { get; set; }
    public int? ResourceId { get; set; }
    public int? FacilityId { get; set; }
    public required DateTime StartDatetime { get; set; }
    public DateTime? EndDatetime { get; set; }
    public int MaxBookings { get; set; } = 1;
}
