using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.Slot.GetContracts;

public class Endpoint(AppDbContext dbContext) : Endpoint<SlotGetContractsRequest, List<SlotGetContractsResponse>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/slot/contracts/{Id}");
        Description(x => x.WithName("SlotGetContracts"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(SlotGetContractsRequest req, CancellationToken ct)
    {
        // First compute the "first" SlotContract Id for each logical group on the server side.
        // Use aggregate (Min) to make this translatable to SQL, then fetch the full records.
        var groupedIds = await _dbContext.SlotContract
            .Where(sc => sc.SlotId == req.Id || sc.Slot.GroupId == req.Id)
            .GroupBy(sc => new
            {
                sc.ContractId,
                sc.Price,
                sc.ValidationId,
                sc.CanPayLater,
                sc.Description
            })
            .Select(g => g.Min(x => x.Id))
            .ToListAsync(ct);

        var slotContracts = await _dbContext.SlotContract
            .Where(sc => groupedIds.Contains(sc.Id))
            .Select(sc => new SlotGetContractsResponse
            {
                Id = sc.Id,
                SlotId = sc.SlotId,
                ContractId = sc.ContractId,
                ContractName = sc.Contract.Name,
                Price = sc.Price,
                ValidationId = sc.ValidationId,
                CanPayLater = sc.CanPayLater,
                Description = sc.Description
            })
            .OrderBy(sc => sc.Price)
            .ToListAsync(ct);

        await Send.OkAsync(slotContracts, ct);
    }
}
