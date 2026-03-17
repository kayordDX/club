using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.Slot.Available;

public class Endpoint(AppDbContext dbContext) : Endpoint<AvailableSlotRequest, bool>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/slot/available");
        Description(x => x.WithName("SlotAvailable"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(AvailableSlotRequest r, CancellationToken ct)
    {
        bool result = false;

        // TypeId 1 == Slot, 2 == Group
        if (r.TypeId == 1)
        {
            var slotContract = await _dbContext.SlotContract.FirstOrDefaultAsync(x => x.SlotId == r.Id, ct);
            result = slotContract != null && !_dbContext.SlotContractBooking.Any(x => x.SlotContractId == slotContract.Id);
        }

        if (r.TypeId == 2)
        {
            // Get all slot contract IDs for slots in this group
            var slotContractIds = await _dbContext.SlotContract
                .Where(sc => _dbContext.Slot
                    .Where(s => s.GroupId == r.Id)
                    .Select(s => s.Id)
                    .Contains(sc.SlotId))
                .Select(sc => sc.Id)
                .ToListAsync(ct);

            int maxCount = await _dbContext.Slot
                .Where(s => s.GroupId == r.Id)
                .CountAsync(ct);

            // Count how many of those slot contracts are already booked
            int bookedCount = await _dbContext.SlotContractBooking
                .CountAsync(scb => slotContractIds.Contains(scb.SlotContractId), ct);
            int requested = r.SlotCount ?? 1;
            result = (maxCount - bookedCount) >= requested;
        }
        await Send.OkAsync(result, ct);
    }
}
