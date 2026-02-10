namespace Online.Entities;

public class Facility : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int OutletId { get; set; }
    public required Outlet Outlet { get; set; }
    public bool? IsActive { get; set; }
    public ICollection<Resource> Resources { get; set; } = [];
    public ICollection<Slot> Slots { get; set; } = [];
    public ICollection<FacilityExtra> FacilityExtras { get; set; } = [];
}
