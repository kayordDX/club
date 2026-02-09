namespace Online.Entities;

public class Outlet : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int BusinessId { get; set; }
    public required Business Business { get; set; }
    public required string VatNumber { get; set; }
    public string? Logo { get; set; }
    public string? Address { get; set; }
    public string? Company { get; set; }
    public string? Registration { get; set; }
    public required string DisplayName { get; set; }
    public int? OutletTypeId { get; set; }
    public OutletType? OutletType { get; set; }
    public int IsActive { get; set; }

    // Navigation properties
    public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
    public ICollection<Extra> Extras { get; set; } = new List<Extra>();
    public ICollection<ContractOutlet> ContractOutlets { get; set; } = new List<ContractOutlet>();
}
