using Club.Data;
using Club.Entities;
using Club.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.CreateMember;

public class Endpoint(AppDbContext dbContext, UserManager<User> userManager, ICustomKeycloakService keycloakService)
    : Endpoint<AdminContractCreateMemberRequest>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly UserManager<User> _userManager = userManager;
    private readonly ICustomKeycloakService _keycloakService = keycloakService;

    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/contract/{Id}/member/create");
        Description(x => x.WithName("AdminContractCreateMember"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractCreateMemberRequest req, CancellationToken ct)
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

        if (req.EndDate.Date < DateTime.UtcNow.Date)
        {
            AddError(r => r.EndDate, "End date cannot be before the start date.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var email = req.Email.Trim();

        // Guard against creating a duplicate: a profile with this email may already exist locally or
        // in Keycloak.
        var existingLocal = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existingLocal is not null)
        {
            AddError(r => r.Email, "A user with this email already exists. Search for them instead.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var existingKeycloak = await _keycloakService.FindUserAsync(email, ct);
        if (existingKeycloak is not null)
        {
            AddError(r => r.Email, "A user with this email already exists. Search for them instead.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        // Create the new profile in Keycloak (sends the account-setup email so the member sets their
        // own password), then mirror it locally and link it to the contract.
        var userId = await _keycloakService.CreateUserAsync(email, req.FirstName.Trim(), req.LastName.Trim(), req.PhoneNumber?.Trim(), ct);

        var user = new User
        {
            Id = userId,
            UserName = email,
            Email = email,
            PhoneNumber = req.PhoneNumber?.Trim(),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            LastSync = DateTime.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            AddError(r => r.Email, "Failed to create the local user profile.");
            await Send.ErrorsAsync(500, ct);
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
                EndDate = req.EndDate.Date,
                Price = contract.Price,
                IsActive = true,
            }
        );

        await _dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
