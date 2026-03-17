using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online.Common;
using Online.Common.Config;
using Online.Data;

namespace Online.Features.Slot.GetAll;

public class Endpoint(AppDbContext dbContext, IOptions<AppConfig> appConfig) : Endpoint<SlotGetAllRequest, List<SlotGetAllResponse>>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly AppConfig _appConfig = appConfig.Value;

    public override void Configure()
    {
        Get("/slot");
        Description(x => x.WithName("SlotGetAll"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(SlotGetAllRequest req, CancellationToken ct)
    {
        var pendingCutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(_appConfig.PendingTimeoutMinutes));

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

        var slotResponses = await _dbContext.Slot
            .Where(s => s.FacilityId == req.FacilityId &&
                        s.StartDatetime >= dateStart &&
                        s.StartDatetime < dateEnd)
            .OrderBy(s => s.StartDatetime)
            .Select(s => new
            {
                s.Id,
                s.SlotGroupId,
                s.FacilityId,
                s.ResourceId,
                s.StartDatetime,
                s.EndDatetime,
                s.RequiresLogin,
                Resource = s.Resource,
                SlotGroup = s.SlotGroupId.HasValue
                    ? _dbContext.SlotGroup
                        .Where(sg => sg.Id == s.SlotGroupId.Value)
                        .Select(sg => new
                        {
                            sg.Id,
                            sg.CanBookForGuests,
                            sg.FacilityId,
                            sg.ResourceId,
                            ResourceName = sg.Resource != null ? sg.Resource.Name : null
                        })
                        .FirstOrDefault()
                    : null,
                SlotContracts = s.SlotContracts.Select(sc => new
                {
                    sc.Id,
                    sc.ContractId,
                    sc.Contract.Name,
                    sc.Price,
                    sc.ValidationId,
                    sc.Validation,
                    sc.CanPayLater,
                    sc.Description
                }).ToList(),
                SlotBookingsCount = s.SlotBookings.Count(sb =>
                    sb.BookingStatusId == BookingStatuses.ConfirmedId ||
                    (sb.BookingStatusId == BookingStatuses.PendingId &&
                     sb.BookingStatusDate >= pendingCutoff))
            })
            .ToListAsync(ct);

        var result = slotResponses
            .GroupBy(s => s.SlotGroupId ?? s.Id)
            .Select(group => new SlotGetAllResponse
            {
                Id = group.Key,
                FacilityId = group.First().SlotGroup?.FacilityId ?? group.First().FacilityId,
                ResourceId = group.First().SlotGroup?.ResourceId ?? group.First().ResourceId,
                ResourceName = group.First().SlotGroup?.ResourceName ?? group.First().Resource?.Name,
                StartDatetime = DateTime.SpecifyKind(group.First().StartDatetime, DateTimeKind.Utc),
                EndDatetime = group.First().EndDatetime,
                Booked = group.Sum(g => g.SlotBookingsCount),
                Total = group.Count(),
                CanBookForGuests = group.First().SlotGroup?.CanBookForGuests ?? false,
                RequiresLogin = group.Any(g => g.RequiresLogin),
                SlotGroup = group.First().SlotGroup is null
                    ? null
                    : new SlotGroupResponse
                    {
                        Id = group.First().SlotGroup!.Id,
                        FacilityId = group.First().SlotGroup!.FacilityId,
                        ResourceId = group.First().SlotGroup!.ResourceId,
                        ResourceName = group.First().SlotGroup!.ResourceName,
                        CanBookForGuests = group.First().SlotGroup!.CanBookForGuests
                    }
            })
            .Where(x => x.IsAvailable)
            .ToList();

        await Send.OkAsync(result, ct);
    }
}
