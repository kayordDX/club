namespace Online.Entities;

public class Resource
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
}