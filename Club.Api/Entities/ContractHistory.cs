namespace Club.Entities;

public class ContractHistory : AuditableEntity
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Frequency { get; set; } = 12;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
}
