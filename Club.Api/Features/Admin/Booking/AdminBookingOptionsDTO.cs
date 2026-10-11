using Club.Features.Slot.GetContracts;

namespace Club.Features.Admin.Booking;

public class AdminBookingOptionsDTO
{
    public List<SlotGetContractsResponse> Contracts { get; set; } = [];
    public List<AdminBookingExtraOptionDTO> Extras { get; set; } = [];
}

public class AdminBookingExtraOptionDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
}
