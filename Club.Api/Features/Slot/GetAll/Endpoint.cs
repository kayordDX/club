using Club.Common.Enums;
using Club.Data;
using Club.Features.Booking.Common;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Slot.GetAll;

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
            _ => req.Date,
        };

        // Get the start and end of the selected date in UTC
        var dateStart = new DateTime(dateUtc.Year, dateUtc.Month, dateUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var dateEnd = dateStart.AddDays(1);

        var now = DateTime.UtcNow;

        // Keep the reservation join aligned with ReservationRules.ActiveReservations; the HTTP state matrix covers both query implementations.
        var result = await _dbContext
            .Database.SqlQuery<SlotGetAllResponse>(
                $"""
                SELECT
                    s.id,
                    s.facility_id,
                    s.resource_id,
                    r.name resource_name,
                    s.start_datetime,
                    s.end_datetime,
                    CAST(COUNT(b.id) AS integer) booked,
                    s.max_bookings total,
                    s.start_datetime >= {now} is_enabled
                FROM slot s
                LEFT JOIN resource r
                    ON r.id = s.resource_id
                LEFT JOIN slot_contract sc
                    ON sc.slot_id = s.id
                LEFT JOIN slot_contract_booking scb
                    ON scb.slot_contract_id = sc.id
                LEFT JOIN booking b
                    ON b.id = scb.booking_id
                    AND (b.booking_status_id = {(int)BookingStatusEnum.Confirmed}
                         OR (b.booking_status_id = {(int)BookingStatusEnum.Pending} AND b.expires_at > {now}))
                WHERE s.facility_id = {req.FacilityId}
                    AND s.start_datetime >= {dateStart}
                    AND s.start_datetime < {dateEnd}
                GROUP BY s.id, r.name
                ORDER BY s.start_datetime
                """
            )
            .ToListAsync(ct);

        if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var slotIds = result.Select(s => s.Id).ToList();
            var players = await _dbContext
                .SlotContractBooking.ActiveReservations(now)
                .Where(scb => slotIds.Contains(scb.SlotContract.SlotId))
                .Select(scb => new { scb.SlotContract.SlotId, scb.Name })
                .ToListAsync(ct);

            foreach (var slot in result)
                slot.PlayerNames = players.Where(p => p.SlotId == slot.Id && !string.IsNullOrWhiteSpace(p.Name)).Select(p => p.Name!).ToList();
        }

        await Send.OkAsync(result, ct);
    }
}
