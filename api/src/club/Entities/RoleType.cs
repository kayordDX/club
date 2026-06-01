namespace Online.Entities;

public class RoleType : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsSuperAdmin { get; set; }
}
