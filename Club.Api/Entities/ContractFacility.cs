namespace Club.Entities;

public class ContractFacility
{
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public int FacilityId { get; set; }
    public required Facility Facility { get; set; }
}
