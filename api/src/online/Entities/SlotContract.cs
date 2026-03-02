namespace Online.Entities;

public class SlotContract : AuditableEntity
{
    public int Id { get; set; }
    public Guid SlotId { get; set; }
    public Slot Slot { get; set; } = default!;
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = default!;
    public decimal Price { get; set; }
    public int? ValidationId { get; set; }
    public Validation? Validation { get; set; }
}
