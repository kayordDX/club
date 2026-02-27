using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.Slot.Get;

public class Endpoint(AppDbContext dbContext) : Endpoint<Request, List<Response>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/slot");
        Description(x => x.WithName("GetSlots"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
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

        var slots = await _dbContext.Slot
            .Where(s => s.FacilityId == req.FacilityId &&
                        s.StartDatetime >= dateStart &&
                        s.StartDatetime < dateEnd)
            .Include(s => s.Resource)
            .Include(s => s.SlotBookings)
            .OrderBy(s => s.StartDatetime)
            .ToListAsync(ct);

        var slotResponses = slots.Select(slot => new Response
        {
            Id = slot.Id,
            FacilityId = slot.FacilityId,
            ResourceId = slot.ResourceId,
            ResourceName = slot.Resource?.Name,
            StartDatetime = DateTime.SpecifyKind(slot.StartDatetime, DateTimeKind.Utc),
            EndDatetime = DateTime.SpecifyKind(slot.EndDatetime, DateTimeKind.Utc),
            GroupId = slot.GroupId,
            AvailableSpots = CalculateAvailableSpots(slot),
            TotalSpots = slot.SlotBookings?.Count ?? 0,
        }).ToList();

        await Send.OkAsync(slotResponses, ct);
    }

    private static int CalculateAvailableSpots(Entities.Slot slot)
    {
        var bookedCount = slot.SlotBookings?.Count ?? 0;
        return bookedCount;
    }
}
