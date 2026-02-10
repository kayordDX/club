namespace Online.Entities;

public class ResourceSlotConfig : AuditableEntity
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public required Resource Resource { get; set; }
    public int SlotConfigId { get; set; }
    public required SlotConfig SlotConfig { get; set; }
    public bool IsActive { get; set; }
}
