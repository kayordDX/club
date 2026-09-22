using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.Update;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractUpdateRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Put("/admin/facility/{FacilityId}/contract/{Id}");
        Description(x => x.WithName("AdminContractUpdate"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractUpdateRequest req, CancellationToken ct)
    {
        var contract = await _dbContext
            .Contract.Where(c => c.Id == req.Id && c.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId))
            .FirstOrDefaultAsync(ct);

        if (contract is null)
        {
            AddError(r => r.Id, "Contract not found.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        contract.Name = req.Name;
        contract.Price = req.Price;
        contract.Frequency = req.Frequency;
        contract.StartDate = req.StartDate;
        contract.EndDate = req.EndDate;
        contract.IsActive = req.IsActive;
        contract.IsPublic = req.IsPublic;

        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
