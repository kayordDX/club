using Club.Data;
using Club.Features.Admin.Contract;
using Club.Services;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.SearchMember;

public class Endpoint(AppDbContext dbContext, ICustomKeycloakService keycloakService) : Endpoint<AdminContractSearchMemberRequest, AdminMemberSearchResultDTO?>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ICustomKeycloakService _keycloakService = keycloakService;

    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/contract/{Id}/member/search");
        Description(x => x.WithName("AdminContractSearchMember"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractSearchMemberRequest req, CancellationToken ct)
    {
        var contract = await _dbContext.Contract.FirstOrDefaultAsync(
            c => c.Id == req.Id && c.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId),
            ct
        );

        if (contract is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var query = req.Query.Trim();

        // Prefer a local match (already-synced users), then fall back to Keycloak.
        var localUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == query || u.PhoneNumber == query, ct);

        Guid? userId = localUser?.Id;
        string? firstName = localUser?.FirstName;
        string? lastName = localUser?.LastName;
        string? email = localUser?.Email;
        string? phone = localUser?.PhoneNumber;

        if (localUser is null)
        {
            var keycloakUser = await _keycloakService.FindUserAsync(query, ct);
            if (keycloakUser is null)
            {
                // Not found — respond 200 with an empty body so the UI can offer to create a profile.
                await Send.OkAsync(null, ct);
                return;
            }

            userId = keycloakUser.Id;
            firstName = keycloakUser.FirstName;
            lastName = keycloakUser.LastName;
            email = keycloakUser.Email;
            phone = keycloakUser.PhoneNumber;
        }

        var isExistingMember = await _dbContext.UserContract.AnyAsync(uc => uc.ContractId == req.Id && uc.UserId == userId, ct);

        await Send.OkAsync(
            new AdminMemberSearchResultDTO
            {
                UserId = userId!.Value,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phone,
                IsExistingMember = isExistingMember,
            },
            ct
        );
    }
}
