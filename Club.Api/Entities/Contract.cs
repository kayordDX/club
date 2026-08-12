namespace Club.Entities;

public class Contract : AuditableEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Frequency { get; set; } = 12;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<ContractFacility> ContractFacilities { get; set; } = [];
}
