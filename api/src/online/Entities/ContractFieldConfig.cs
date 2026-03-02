namespace Online.Entities;

public class ContractFieldConfig : AuditableEntity
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public int ContractConfigId { get; set; }
    public required ContractField ContractField { get; set; }
    public bool IsActive { get; set; }
}
