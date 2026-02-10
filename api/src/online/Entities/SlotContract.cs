namespace Online.Entities;

public class SlotContract : AuditableEntity
{
    public int Id { get; set; }
    public Guid SlotId { get; set; }
    public required Slot Slot { get; set; }
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public decimal Price { get; set; }
    public int ValidationId { get; set; }
    public required Validation Validation { get; set; }
}
