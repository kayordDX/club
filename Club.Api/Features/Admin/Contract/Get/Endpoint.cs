using Club.Data;
using Club.Features.Admin.Contract;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.Get;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractGetRequest, AdminContractDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/contract/{Id}");
        Description(x => x.WithName("AdminContractGet"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractGetRequest req, CancellationToken ct)
    {
        var contract = await _dbContext
            .Contract.Where(c => c.Id == req.Id && c.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId))
            .Select(c => new AdminContractDTO
            {
                Id = c.Id,
                Name = c.Name,
                Price = c.Price,
                Frequency = c.Frequency,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive,
                IsPublic = c.IsPublic,
            })
            .FirstOrDefaultAsync(ct);

        if (contract is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(contract, ct);
    }
}
