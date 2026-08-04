namespace Club.Entities;

public class Business : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Outlet> Outlets { get; set; } = [];
}
