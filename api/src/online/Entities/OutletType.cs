namespace Online.Entities;

public class OutletType : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
