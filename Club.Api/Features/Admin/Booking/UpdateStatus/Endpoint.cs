using Club.Common;
using Club.Common.Config;
using Club.Common.Enums;
using Club.Data;
using Club.Features.Booking.Common;
using Microsoft.Extensions.Options;

namespace Club.Features.Admin.Booking.UpdateStatus;

public class Endpoint(AppDbContext dbContext, IOptions<AppConfig> appConfig) : Endpoint<AdminBookingUpdateStatusRequest>
{
    public override void Configure()
    {
        Put("/admin/facility/{FacilityId}/booking/{Id}/status");
        Description(x => x.WithName("AdminBookingUpdateStatus"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingUpdateStatusRequest req, CancellationToken ct)
    {
        var actorId = Helpers.GetCurrentUserId(HttpContext);
        if (!actorId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        if (!Enum.IsDefined(req.Status))
        {
            AddError(r => r.Status, "Invalid status.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var failure = await BookingStatusMutation.ChangeAsync(
            dbContext,
            req.Id,
            actorId.Value,
            req.FacilityId,
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
