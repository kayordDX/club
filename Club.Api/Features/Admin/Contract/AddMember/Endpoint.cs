using Club.Data;
using Club.Entities;
using Club.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.AddMember;

public class Endpoint(AppDbContext dbContext, UserManager<User> userManager, ICustomKeycloakService keycloakService) : Endpoint<AdminContractAddMemberRequest>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICustomKeycloakService _keycloakService = keycloakService;

    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/contract/{Id}/member");
        Description(x => x.WithName("AdminContractAddMember"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractAddMemberRequest req, CancellationToken ct)
    {
        var contract = await _dbContext.Contract.FirstOrDefaultAsync(
            c => c.Id == req.Id && c.ContractFacilities.Any(cf => cf.FacilityId == req.FacilityId),
            ct
        );

        if (contract is null)
        {
            AddError(r => r.Id, "Contract not found.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        // The user may exist only in Keycloak (found via search but never logged in); mirror them into
        // the local user table so the UserContract FK is valid.
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
        if (user is null)
        {
            var keycloakUser = await _keycloakService.GetUserAsync(req.UserId, ct);
            if (keycloakUser is null)
            {
                AddError(r => r.UserId, "User not found.");
                await Send.ErrorsAsync(404, ct);
                return;
            }

            user = new User
            {
                Id = keycloakUser.Id,
                UserName = keycloakUser.Email,
                Email = keycloakUser.Email,
                PhoneNumber = keycloakUser.PhoneNumber,
                FirstName = keycloakUser.FirstName ?? keycloakUser.Email ?? "",
                LastName = keycloakUser.LastName ?? "",
                LastSync = DateTime.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                AddError(r => r.UserId, "Failed to create the local user profile.");
                await Send.ErrorsAsync(500, ct);
                return;
            }
        }

        var alreadyMember = await _dbContext.UserContract.AnyAsync(uc => uc.ContractId == req.Id && uc.UserId == req.UserId, ct);
        if (alreadyMember)
        {
            AddError(r => r.UserId, "This user is already a member of the contract.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        _dbContext.UserContract.Add(
            new UserContract
            {
                ContractId = contract.Id,
                Contract = contract,
                UserId = user.Id,
                User = user,
                StartDate = DateTime.UtcNow,
                Price = contract.Price,
                IsActive = true,
            }
        );

        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
