namespace Online.DTO;

public class OutletDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int BusinessId { get; set; }
    public required BusinessDTO Business { get; set; }
    public required string VatNumber { get; set; }
    public string? Logo { get; set; }
    public string? Address { get; set; }
    public string? Company { get; set; }
    public string? Registration { get; set; }
    public required string DisplayName { get; set; }
    public int OutletTypeId { get; set; }
    public OutletTypeDTO OutletType { get; set; } = default!;
    public int IsActive { get; set; }
    public ICollection<FacilityDTO> Facilities { get; set; } = [];
}
