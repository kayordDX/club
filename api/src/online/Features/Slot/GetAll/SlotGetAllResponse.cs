using Online.Entities;

namespace Online.Features.Slot.GetAll;

public class SlotGetAllResponse
{
    public Guid Id { get; set; }
    public bool IsGroup { get; set; }
    public int? FacilityId { get; set; }
    public int? ResourceId { get; set; }
    public string? ResourceName { get; set; }
    public DateTime StartDatetime { get; set; }
    public DateTime? EndDatetime { get; set; }
    public int AvailableSpots { get; set; }
    public int TotalSpots { get; set; }
    public decimal Price { get; set; }
    public List<SlotContractResponse> SlotContracts { get; set; } = [];
}

public class SlotContractResponse
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string ContractName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? ValidationId { get; set; }
    public Validation? Validation { get; set; }
    public bool CanPayLater { get; set; }
    public string? Description { get; set; }
}
