using Club.Common;
using Club.Common.Config;
using Club.Common.Enums;
using Club.Data;
using Club.Features.Booking.Common;
using Microsoft.Extensions.Options;

namespace Club.Features.Booking.UpdateStatus;

public class Endpoint(AppDbContext dbContext, IOptions<AppConfig> appConfig) : Endpoint<BookingUpdateStatusRequest>
{
    public override void Configure()
    {
        Put("/booking/status");
        Description(x => x.WithName("BookingUpdateStatus"));
    }

    public override async Task HandleAsync(BookingUpdateStatusRequest req, CancellationToken ct)
    {
        var actorId = Helpers.GetCurrentUserId(HttpContext);
        if (!actorId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        if (req.Status != BookingStatusEnum.Cancelled && req.Status != BookingStatusEnum.Confirmed)
        {
            AddError(r => r.Status, "Invalid status.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var failure = await BookingStatusMutation.ChangeAsync(
            dbContext,
            req.BookingId,
            actorId.Value,
            null,
            req.Status,
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
