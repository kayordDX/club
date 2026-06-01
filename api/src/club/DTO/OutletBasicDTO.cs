namespace Club.DTO;

public class OutletBasicDTO
{
    public int Id { get; set; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string BusinessName { get; set; }
    public string? Logo { get; set; }
    public required string DisplayName { get; set; }
    public int OutletTypeId { get; set; }
    public OutletTypeDTO OutletType { get; set; } = default!;
    public bool IsActive { get; set; }
    public ICollection<FacilityBasicDTO> Facilities { get; set; } = [];
}
