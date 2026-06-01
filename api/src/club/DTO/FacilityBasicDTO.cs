namespace Club.DTO;

public class FacilityBasicDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool? IsActive { get; set; }
    public int FacilityTypeId { get; set; }
    public required string FacilityTypeName { get; set; }
}
