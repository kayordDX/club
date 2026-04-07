using Microsoft.EntityFrameworkCore;
using Online.Common.Enums;
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

    [TickerFunction("ClearExpiredBookings", "0 * * * * *")]
    public async Task ClearExpiredBookings(CancellationToken ct)
    {
        await _dbContext.Database.ExecuteSqlAsync($"""
            WITH expired AS (
              SELECT id
              FROM booking
              WHERE booking_status_id = 2
                AND expires_at <= now()
            ),
            updated AS (
              UPDATE booking
              SET booking_status_id = 4
              WHERE id IN (SELECT id FROM expired)
              RETURNING id
            )
            DELETE FROM slot_contract_booking
            WHERE booking_id IN (SELECT id FROM updated);
        """, ct);
    }
}
