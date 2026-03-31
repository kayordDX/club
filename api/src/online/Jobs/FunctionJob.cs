using Microsoft.EntityFrameworkCore;
using Online.Data;
using TickerQ.Utilities.Base;

namespace Online.Jobs;

public class FunctionJob(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    [TickerFunction("Sql")]
    public async Task RawSql(TickerFunctionContext<string> tickerContext, CancellationToken ct)
    {
        _dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(2));
        var sql = tickerContext.Request;
        if (sql == null) return;
        await _dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
