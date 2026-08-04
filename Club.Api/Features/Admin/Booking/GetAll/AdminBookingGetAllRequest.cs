using Club.Common.Models;

namespace Club.Features.Admin.Booking.GetAll;

public class AdminBookingGetAllRequest : QueryModel
{
    public int FacilityId { get; set; }
}
