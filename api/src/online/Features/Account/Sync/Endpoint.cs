using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Online.Common;
using Online.Common.Config;
using Online.Entities;

namespace Online.Features.Account.Sync;

public class Endpoint(IKeycloakUserClient keycloakUserClient, IOptions<KeycloakConfig> keycloakConfig, UserManager<User> userManager) : Endpoint<AccountSyncRequest>
{
    private readonly KeycloakConfig keycloakConfig = keycloakConfig.Value;

    public override void Configure()
    {
        Post("/account/sync");
        Description(x => x.WithName("AccountSync"));
    }

    public override async Task HandleAsync(AccountSyncRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        bool shouldSync = user == null || user.LastSync.AddHours(1) < DateTime.Now;

        if (!shouldSync)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var keycloakUser = await keycloakUserClient.GetUserAsync(keycloakConfig.Realm, userId.Value.ToString(), cancellationToken: ct);
        if (user != null)
        {
            var updatedUser = MapKeycloakToUser(userId.Value, user, keycloakUser);
            var result = await userManager.UpdateAsync(updatedUser);
            if (result.Succeeded)
            {
                await Send.NoContentAsync(ct);
            }
            else
            {
                await Send.NotFoundAsync(ct);
            }
        }

        else
        {
            // create new user
            var newUser = MapKeycloakToUser(userId.Value, null, keycloakUser);
            var result = await userManager.CreateAsync(newUser);
            if (result.Succeeded)
            {
                await Send.NoContentAsync(ct);
            }
            else
            {
                await Send.ErrorsAsync(500, ct);
            }
        }
    }

    private static User MapKeycloakToUser(Guid userId, User? user, UserRepresentation keycloakUser)
    {
        var picture = keycloakUser.Attributes?.FirstOrDefault(x => x.Key == "picture").Value?.FirstOrDefault();
        var phoneNumber = keycloakUser.Attributes?.FirstOrDefault(x => x.Key == "phoneNumber").Value?.FirstOrDefault();
        var phoneNumberVerified = keycloakUser.Attributes?.FirstOrDefault(x => x.Key == "phoneNumberVerified").Value?.FirstOrDefault() == "true";

        user ??= new User
        {
            FirstName = keycloakUser.FirstName ?? "",
            LastName = keycloakUser.LastName ?? "",
        };

        user.Id = userId;
        user.TwoFactorEnabled = keycloakUser.Totp ?? false;
        user.Email = keycloakUser.Email;
        user.EmailConfirmed = keycloakUser.EmailVerified ?? false;
        user.UserName = keycloakUser.Username;
        user.FirstName = keycloakUser.FirstName ?? keycloakUser.Username ?? "";
        user.LastName = keycloakUser.LastName ?? "";
        user.Picture = picture;
        user.PhoneNumber = phoneNumber;
        user.PhoneNumberConfirmed = phoneNumberVerified;
        user.LastSync = DateTime.UtcNow;

        return user;
    }
}
