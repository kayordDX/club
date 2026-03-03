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

        var slots = await _dbContext.Slot
            .Where(s => s.FacilityId == req.FacilityId &&
                        s.StartDatetime >= dateStart &&
                        s.StartDatetime < dateEnd)
            .Include(s => s.Resource)
            .Include(s => s.SlotBookings)
            .OrderBy(s => s.StartDatetime)
            .ToListAsync(ct);

        var slotResponses = slots
            .GroupBy(s => s.GroupId ?? s.Id)
            .Select(group => new SlotGetAllResponse
            {
                Id = group.Key,
                IsGroup = group.First().GroupId.HasValue,
                FacilityId = group.First().FacilityId,
                ResourceId = group.First().ResourceId,
                ResourceName = group.First().Resource?.Name,
                StartDatetime = DateTime.SpecifyKind(group.First().StartDatetime, DateTimeKind.Utc),
                EndDatetime = group.First().EndDatetime,
                AvailableSpots = group.Sum(s => CalculateAvailableSpots(s)),
                TotalSpots = group.Sum(s => s.SlotBookings?.Count ?? 0),
            }).ToList();

        await Send.OkAsync(slotResponses, ct);
    }

    private static int CalculateAvailableSpots(Entities.Slot slot)
    {
        var bookedCount = slot.SlotBookings?.Count ?? 0;
        return bookedCount;
    }
}
