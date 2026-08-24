namespace Club.Features.Admin.Slot.GetAll;

public class AdminSlotGetAllRequest
{
    public int FacilityId { get; set; }
    public required DateTime Date { get; set; }
}
