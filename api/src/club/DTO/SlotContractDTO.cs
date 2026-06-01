namespace Online.DTO;

public class SlotContractDTO
{
    public int Id { get; set; }
    public Guid SlotId { get; set; }
    public SlotDTO Slot { get; set; } = default!;
    public int ContractId { get; set; }
    public decimal Price { get; set; }
    public int? ValidationId { get; set; }
    public bool CanPayLater { get; set; }
    public string? Description { get; set; }
}
