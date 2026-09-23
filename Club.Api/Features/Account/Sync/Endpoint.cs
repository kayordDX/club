using Club.Common;
using Club.Services;

namespace Club.Features.Account.Sync;

public class Endpoint(UserSyncService userSyncService) : Endpoint<AccountSyncRequest>
{
    public override void Configure()
    {
        Post("/account/sync");
        Description(x => x.WithName("AccountSync"));
    }

    public override async Task HandleAsync(AccountSyncRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await userSyncService.SyncUserByIdAsync(userId.Value, req.Force, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!result.Succeeded)
        {
            await Send.ErrorsAsync(500, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
