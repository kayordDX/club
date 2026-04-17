using OpenIddict.Abstractions;
using TickerQ.Utilities.Base;

namespace Online.Jobs;

public class OpenIddictCleanupJob(IOpenIddictTokenManager tokenManager)
{
    private readonly IOpenIddictTokenManager _tokenManager = tokenManager;

    [TickerFunction("OpenIddictCleanup", "0 0 1 * * *")]
    public async Task RawSql(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-1);
        await _tokenManager.PruneAsync(threshold, ct);
    }
}
