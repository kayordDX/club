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
        var slotContracts = await _dbContext.SlotContract
            .Where(sc => sc.SlotId == req.Id)
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
            .ToListAsync(ct);

        await Send.OkAsync(slotContracts, ct);
    }
}
