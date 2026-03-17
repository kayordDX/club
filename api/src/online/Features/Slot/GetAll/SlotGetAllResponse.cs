using Online.Entities;

namespace Online.Features.Slot.GetAll;

public class SlotGetAllResponse
{
    public Guid Id { get; set; }
    public Guid? GroupId { get; set; }
    public int? FacilityId { get; set; }
    public int? ResourceId { get; set; }
    public string? ResourceName { get; set; }
    public DateTime StartDatetime { get; set; }
    public DateTime? EndDatetime { get; set; }
    public int Booked { get; set; }
    public int Total { get; set; }
    public bool CanBookForGuests { get; set; }
    public bool RequiresLogin { get; set; }
    public bool IsAvailable => Booked < Total;
}
