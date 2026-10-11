namespace Club.Features.Admin.Booking.SearchCustomer;

public class AdminBookingSearchCustomerRequest
{
    public int FacilityId { get; set; }
    public string Query { get; set; } = "";
}
