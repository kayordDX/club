namespace Online.Entities;

public class ContractOutlet : AuditableEntity
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public int OutletId { get; set; }
    public required Outlet Outlet { get; set; }
    public DateTime? ContractStart { get; set; }
    public DateTime? ContractEnd { get; set; }
    public bool IsActive { get; set; }
}
