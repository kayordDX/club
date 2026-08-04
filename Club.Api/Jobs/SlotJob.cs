using Club.Data;
using TickerQ.Utilities.Base;

namespace Club.Jobs;

public class SlotJob(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    [TickerFunction("CreateSlots")]
    public async Task CreateSlotsForWeek(CancellationToken ct)
    {
        await SeedDbContext.EnsureSlotCoverage(_dbContext, ct);
    }
}
