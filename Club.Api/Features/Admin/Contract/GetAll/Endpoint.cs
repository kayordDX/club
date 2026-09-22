using Club.Data;
using Club.Features.Admin.Contract;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.GetAll;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractGetAllRequest, List<AdminContractDTO>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/contract");
        Description(x => x.WithName("AdminContractGetAll"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractGetAllRequest req, CancellationToken ct)
    {
        var contracts = await _dbContext
            .Contract.Where(c => c.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId))
            .OrderBy(c => c.Name)
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
            .ToListAsync(ct);

        await Send.OkAsync(contracts, ct);
    }
}
