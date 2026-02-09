namespace Online.Entities;

public class Extra
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int OutletId { get; set; }
    public required Outlet Outlet { get; set; }

    // Navigation properties
    public ICollection<FacilityExtra> FacilityExtras { get; set; } = new List<FacilityExtra>();
}
