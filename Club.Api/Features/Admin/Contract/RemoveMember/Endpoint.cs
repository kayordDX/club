using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.RemoveMember;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractRemoveMemberRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Delete("/admin/facility/{FacilityId}/contract/{Id}/member/{MemberId}");
        Description(x => x.WithName("AdminContractRemoveMember"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractRemoveMemberRequest req, CancellationToken ct)
    {
        var userContract = await _dbContext
            .UserContract.Where(uc =>
                uc.Id == req.MemberId && uc.ContractId == req.Id && uc.Contract.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId)
            )
            .FirstOrDefaultAsync(ct);

        if (userContract is null)
        {
            AddError(r => r.MemberId, "Member not found.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        _dbContext.UserContract.Remove(userContract);
        await _dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
