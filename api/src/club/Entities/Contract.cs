namespace Online.Entities;

public class Contract : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int BusinessId { get; set; }
    public required Business Business { get; set; }
}
