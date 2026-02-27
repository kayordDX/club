namespace Online.Features.Slot.GetAll;

public class SlotGetAllResponse
{
    public Guid Id { get; set; }
    public int? FacilityId { get; set; }
    public int? ResourceId { get; set; }
    public string? ResourceName { get; set; }
    public DateTime StartDatetime { get; set; }
    public DateTime EndDatetime { get; set; }
    public Guid GroupId { get; set; }
    public int AvailableSpots { get; set; }
    public int TotalSpots { get; set; }
}
