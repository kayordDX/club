namespace Online.Entities;

public class Slot
{
    public int Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int ResourceId { get; set; }
    public required Resource Resource { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
}