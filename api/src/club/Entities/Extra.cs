namespace Online.Entities;

public class Extra
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public Facility Facility { get; set; } = default!;
    public int OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsOnline { get; set; }
}
