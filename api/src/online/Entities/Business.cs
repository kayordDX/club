namespace Online.Entities;

public class Business : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Navigation properties
    public ICollection<Outlet> Outlets { get; set; } = new List<Outlet>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<ContractConfig> ContractConfigs { get; set; } = new List<ContractConfig>();
}
