namespace Online.Entities;

public class ContractContractConfig : AuditableEntity
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public int ContractConfigId { get; set; }
    public required ContractConfig ContractConfig { get; set; }
    public bool IsActive { get; set; }
}
