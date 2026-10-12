using Club.Data;
using Club.Features.Booking.Common;
using Club.Features.Booking.GetPath;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Booking.GetPath;

public class Endpoint(AppDbContext db) : Endpoint<AdminBookingGetPathRequest, BookingPathDTO>
{
    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/booking/{Id}/path");
        Description(x => x.WithName("AdminBookingGetPath"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingGetPathRequest req, CancellationToken ct)
    {
        var result = await db.Booking.Where(b => b.Id == req.Id && b.FacilityId == req.FacilityId).ProjectToPath().FirstOrDefaultAsync(ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(result, ct);
    }
}
