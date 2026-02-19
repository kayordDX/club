namespace Online.DTO;

public class FacilityDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int OutletId { get; set; }
    public bool? IsActive { get; set; }
    public int FacilityTypeId { get; set; }
    public FacilityTypeDTO FacilityType { get; set; } = default!;
}
