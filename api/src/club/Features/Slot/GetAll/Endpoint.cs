using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.Slot.GetAll;

public class Endpoint(AppDbContext dbContext) : Endpoint<SlotGetAllRequest, List<SlotGetAllResponse>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/slot");
        Description(x => x.WithName("SlotGetAll"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(SlotGetAllRequest req, CancellationToken ct)
    {
        // Ensure the date is in UTC
        var dateUtc = req.Date.Kind switch
        {
            DateTimeKind.Local => req.Date.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(req.Date, DateTimeKind.Utc),
            _ => req.Date
        };

        // Get the start and end of the selected date in UTC
        var dateStart = new DateTime(dateUtc.Year, dateUtc.Month, dateUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var dateEnd = dateStart.AddDays(1);

        var result = await _dbContext.Database
            .SqlQuery<SlotGetAllResponse>($"""
                SELECT
                    s.id,
                    s.facility_id,
                    s.resource_id,
                    r.name resource_name,
                    s.start_datetime,
                    s.end_datetime,
                    CAST(COUNT(scb.id) AS integer) booked,
                    s.max_bookings total
                FROM slot s
                LEFT JOIN resource r
                    ON r.id = s.resource_id
                LEFT JOIN slot_contract sc
                    ON sc.slot_id = s.id
                LEFT JOIN slot_contract_booking scb
                    ON scb.slot_contract_id = sc.id
                WHERE s.facility_id = {req.FacilityId}
                  AND s.start_datetime >= {dateStart}
                  AND s.start_datetime < {dateEnd}
                GROUP BY s.id, r.name
                ORDER BY s.start_datetime
                """)
            .ToListAsync(ct);

        await Send.OkAsync(result, ct);
    }
}
