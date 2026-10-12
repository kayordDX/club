using Club.Common;
using Club.Data;
using Club.Features.Booking.Common;
using Club.Features.Booking.Create;

namespace Club.Features.Admin.Booking.Update;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminBookingUpdateRequest>
{
    public override void Configure()
    {
        Put("/admin/facility/{FacilityId}/booking/{Id}");
        Description(x => x.WithName("AdminBookingUpdate"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingUpdateRequest req, CancellationToken ct)
    {
        var actorId = Helpers.GetCurrentUserId(HttpContext);
        if (!actorId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var failure = await BookingEditMutation.EditAsync(
            dbContext,
            req.Id,
            new BookingCreateRequest { Bookings = req.Bookings, Extras = req.Extras },
            actorId.Value,
            req.FacilityId,
            0,
            ct
        );
        if (failure is not null)
        {
            AddError(failure.Message);
            await Send.ErrorsAsync(failure.StatusCode, ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
