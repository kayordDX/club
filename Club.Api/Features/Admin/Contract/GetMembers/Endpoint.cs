using Club.Data;
using Club.Features.Admin.Contract;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.GetMembers;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractGetMembersRequest, List<AdminContractMemberDTO>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/contract/{Id}/members");
        Description(x => x.WithName("AdminContractGetMembers"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractGetMembersRequest req, CancellationToken ct)
    {
        var members = await _dbContext
            .UserContract.Where(uc => uc.ContractId == req.Id && uc.Contract.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId))
            .OrderBy(uc => uc.User.LastName)
            .ThenBy(uc => uc.User.FirstName)
            .Select(uc => new AdminContractMemberDTO
            {
                Id = uc.Id,
                FirstName = uc.User.FirstName,
                LastName = uc.User.LastName,
                Email = uc.User.Email,
                StartDate = uc.StartDate,
                EndDate = uc.EndDate,
                IsActive = uc.IsActive,
            })
            .ToListAsync(ct);

        await Send.OkAsync(members, ct);
    }
}
