using Club.Common;
using Club.Common.Config;
using Club.Data;
using Club.Features.Booking.Common;
using Club.Features.Booking.Create;
using Microsoft.Extensions.Options;

namespace Club.Features.Booking.Update;

public class Endpoint(AppDbContext dbContext, IOptions<AppConfig> appConfig) : Endpoint<BookingUpdateRequest>
{
    public override void Configure()
    {
        Put("/booking/{Id}");
        Description(x => x.WithName("BookingUpdate"));
    }

    public override async Task HandleAsync(BookingUpdateRequest req, CancellationToken ct)
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
            null,
            appConfig.Value.PendingTimeoutMinutes,
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
