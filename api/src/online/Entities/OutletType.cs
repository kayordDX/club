namespace Online.Entities;

public class OutletType : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<Outlet> Outlets { get; set; } = new List<Outlet>();
}
