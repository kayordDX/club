namespace Online.Entities;

public class ContractConfig : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? FieldValidation { get; set; }
    public int BusinessId { get; set; }
    public required Business Business { get; set; }

    // Navigation properties
    public ICollection<ContractContractConfig> ContractContractConfigs { get; set; } = new List<ContractContractConfig>();
}
