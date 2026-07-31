using Club.Entities;

namespace Club.DTO;

public class ExtraBookingDTO
{
    public int ExtraId { get; set; }
    public ExtraDTO Extra { get; set; } = default!;
    public int BookingId { get; set; }
    public int Amount { get; set; }
}

public class ExtraDTO
{
    public int Id { get; set; }
    public int FacilityId { get; set; }
    public int OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
