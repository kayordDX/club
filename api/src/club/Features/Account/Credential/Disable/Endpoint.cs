using Microsoft.AspNetCore.Identity;
using Online.Entities;
using Online.Services;

namespace Online.Features.Account.Credential.Disable;

public class Endpoint(ICustomKeycloakService keycloakService, UserManager<User> userManager) : Endpoint<CredentialDisableRequest>
{
    public override void Configure()
    {
        Post("/account/credential/disable");
        Description(x => x.WithName("AccountCredentialDisable"));
    }

    public override async Task HandleAsync(CredentialDisableRequest req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new Exception("User not found");
        bool result = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, req.Token);
        if (!result)
        {
            ValidationContext.Instance.ThrowError("Invalid token", "token");
        }

        var disabled = await keycloakService.DisableUserTotpAsync(user.Id, ct);
        if (!disabled)
        {
            throw new Exception("No active TOTP credential found for this account.");
        }
        await Send.NoContentAsync(ct);
    }
}
