namespace Online.Entities;

public class SlotConfigType : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int BusinessId { get; set; }
}
