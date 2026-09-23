using Club.Services;
using TickerQ.Utilities.Base;

namespace Club.Jobs;

public class UserJob(UserSyncService userSyncService)
{
    [TickerFunction("SyncKeycloakUsers")]
    public async Task SyncKeycloakUsers(CancellationToken ct)
    {
        await userSyncService.SyncAllAsync(ct);
    }
}
