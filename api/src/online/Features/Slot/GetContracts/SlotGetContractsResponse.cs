using Online.Entities;

namespace Online.Features.Slot.GetContracts;

public class SlotGetContractsResponse
{
    public int Id { get; set; }
    public Guid SlotId { get; set; }
    public int ContractId { get; set; }
    public string ContractName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? ValidationId { get; set; }
    public bool CanPayLater { get; set; }
    public string? Description { get; set; }
}
