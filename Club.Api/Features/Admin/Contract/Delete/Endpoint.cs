using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.Delete;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractDeleteRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Delete("/admin/facility/{FacilityId}/contract/{Id}");
        Description(x => x.WithName("AdminContractDelete"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractDeleteRequest req, CancellationToken ct)
    {
        var contract = await _dbContext
            .Contract.Include(c => c.ContractFacilities)
            .Where(c => c.Id == req.Id && c.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId))
            .FirstOrDefaultAsync(ct);

        if (contract is null)
        {
            AddError(r => r.Id, "Contract not found.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        // Protect data integrity: a contract that is already bookable (linked to slots) or held by
        // members must not be removed, otherwise those records would be orphaned.
        var inUse =
            await _dbContext.SlotContract.AnyAsync(sc => sc.ContractId == req.Id, ct)
            || await _dbContext.UserContract.AnyAsync(uc => uc.ContractId == req.Id, ct);

        if (inUse)
        {
            AddError(r => r.Id, "Contract is in use by slots or members and cannot be deleted.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        _dbContext.ContractFacility.RemoveRange(contract.ContractFacilities);
        _dbContext.Contract.Remove(contract);
        await _dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
