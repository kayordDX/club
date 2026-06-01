namespace Online.Entities;

public class ContractField : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? FieldValidation { get; set; }
    public int BusinessId { get; set; }
    public required Business Business { get; set; }
    public ICollection<ContractFieldConfig> ContractFieldConfigs { get; set; } = [];
}
