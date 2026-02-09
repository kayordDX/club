namespace Online.Entities;

public class Contract : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int BusinessId { get; set; }
    public required Business Business { get; set; }

    // Navigation properties
    public ICollection<ContractOutlet> ContractOutlets { get; set; } = new List<ContractOutlet>();
    public ICollection<ContractContractConfig> ContractContractConfigs { get; set; } = new List<ContractContractConfig>();
    public ICollection<SlotContract> SlotContracts { get; set; } = new List<SlotContract>();
    public ICollection<UserContract> UserContracts { get; set; } = new List<UserContract>();
}
