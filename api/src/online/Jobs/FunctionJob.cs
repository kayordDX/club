using Microsoft.EntityFrameworkCore;
using Online.Data;
using TickerQ.Utilities.Base;

namespace Online.Jobs;

public class FunctionJob(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    [TickerFunction("Test")]
    public async Task StockThreshold(CancellationToken ct)
    {
        // _dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(2));
        await _dbContext.Database.ExecuteSqlAsync($"SELECT update_stock_threshold();", ct);
    }
}
