using Club.Data;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.UpdateMember;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractUpdateMemberRequest>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Put("/admin/facility/{FacilityId}/contract/{Id}/member/{MemberId}");
        Description(x => x.WithName("AdminContractUpdateMember"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractUpdateMemberRequest req, CancellationToken ct)
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

        if (req.EndDate.Date < userContract.StartDate.Date)
        {
            AddError(r => r.EndDate, "End date cannot be before the start date.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        userContract.EndDate = req.EndDate.Date;
        await _dbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
