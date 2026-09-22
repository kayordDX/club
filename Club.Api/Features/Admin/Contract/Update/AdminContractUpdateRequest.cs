namespace Club.Features.Admin.Contract.Update;

public class AdminContractUpdateRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Frequency { get; set; } = 12;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
}
