using Club.Common.Enums;
using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Slot.GetAll;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminSlotGetAllRequest, List<AdminSlotGetAllResponse>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/slot");
        Description(x => x.WithName("AdminSlotGetAll"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminSlotGetAllRequest req, CancellationToken ct)
    {
        // Slots are stored against UTC calendar dates, so query the start of the
        // requested date in UTC (mirrors the public SlotGetAll endpoint).
        var dateUtc = req.Date.Kind switch
        {
            DateTimeKind.Local => req.Date.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(req.Date, DateTimeKind.Utc),
            _ => req.Date,
        };

        var dateStart = new DateTime(dateUtc.Year, dateUtc.Month, dateUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var dateEnd = dateStart.AddDays(1);
        var now = DateTime.UtcNow;

        var slots = await _dbContext
            .Slot.Where(s => s.FacilityId == req.FacilityId && s.StartDatetime >= dateStart && s.StartDatetime < dateEnd)
            .OrderBy(s => s.StartDatetime)
            .Select(s => new
            {
                s.Id,
                ResourceName = s.Resource != null ? s.Resource.Name : null,
                s.StartDatetime,
                s.EndDatetime,
                s.MaxBookings,
                IsEnabled = s.StartDatetime >= now,
            })
            .ToListAsync(ct);

        var slotIds = slots.Select(s => s.Id).ToList();

        // Mirrors the SlotGetAll capacity count: cancelled bookings never hold a
        // slot and pending bookings stop holding it once they expire.
        var bookings = await _dbContext
            .SlotContractBooking.Where(scb =>
                slotIds.Contains(scb.SlotContract.SlotId)
                && scb.Booking.BookingStatusId != (int)BookingStatusEnum.Cancelled
                && (scb.Booking.BookingStatusId != (int)BookingStatusEnum.Pending || scb.Booking.ExpiresAt >= now)
            )
            .Select(scb => new
            {
                scb.SlotContract.SlotId,
                scb.BookingId,
                scb.Name,
                BookingStatusId = scb.Booking.BookingStatusId,
                BookingStatusName = scb.Booking.BookingStatus.Name,
            })
            .OrderBy(b => b.BookingId)
            .ToListAsync(ct);

        var result = slots
            .Select(s => new AdminSlotGetAllResponse
            {
                Id = s.Id,
                ResourceName = s.ResourceName,
                StartDatetime = s.StartDatetime,
                EndDatetime = s.EndDatetime,
                Total = s.MaxBookings,
                IsEnabled = s.IsEnabled,
                Booked = bookings.Count(b => b.SlotId == s.Id),
                Bookings = bookings
                    .Where(b => b.SlotId == s.Id)
                    .Select(b => new AdminSlotBookingDTO
                    {
                        BookingId = b.BookingId,
                        PlayerName = b.Name,
                        BookingStatusId = b.BookingStatusId,
                        BookingStatusName = b.BookingStatusName,
                    })
                    .ToList(),
            })
            .ToList();

        await Send.OkAsync(result, ct);
    }
}
