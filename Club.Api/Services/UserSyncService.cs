using Club.Common.Config;
using Club.Entities;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Club.Services;

public class UserSyncService(IKeycloakUserClient keycloakUserClient, IOptions<KeycloakConfig> keycloakConfig, UserManager<User> userManager)
{
    private const int PageSize = 100;
    private readonly IKeycloakUserClient _keycloakUserClient = keycloakUserClient;
    private readonly KeycloakConfig _keycloakConfig = keycloakConfig.Value;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<IdentityResult> SyncUserAsync(UserRepresentation keycloakUser, CancellationToken ct)
    {
        if (keycloakUser.Id is null || !Guid.TryParse(keycloakUser.Id, out var userId))
        {
            return IdentityResult.Failed(new IdentityError { Code = "InvalidKeycloakUserId", Description = "The Keycloak user has no valid id." });
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        var mappedUser = MapKeycloakToUser(userId, user, keycloakUser);

        return user is null ? await _userManager.CreateAsync(mappedUser) : await _userManager.UpdateAsync(mappedUser);
    }

    public async Task<IdentityResult?> SyncUserByIdAsync(Guid userId, bool force, CancellationToken ct)
    {
        var existingUser = await _userManager.FindByIdAsync(userId.ToString());
        if (existingUser is not null && !force && existingUser.LastSync.AddHours(1) >= DateTime.UtcNow)
            return IdentityResult.Success;

        var keycloakUser = await _keycloakUserClient.GetUserAsync(_keycloakConfig.Realm, userId.ToString(), cancellationToken: ct);
        return keycloakUser is null ? null : await SyncUserAsync(keycloakUser, ct);
    }

    public async Task<int> SyncAllAsync(CancellationToken ct)
    {
        var synced = 0;
        for (var first = 0; ; first += PageSize)
        {
            var users = await _keycloakUserClient.GetUsersAsync(
                _keycloakConfig.Realm,
                new GetUsersRequestParameters { First = first, Max = PageSize },
                cancellationToken: ct
            );

            if (users is null || users.Count() == 0)
                return synced;

            foreach (var keycloakUser in users)
            {
                if (keycloakUser is null)
                    continue;

                var result = await SyncUserAsync(keycloakUser, ct);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not sync Keycloak user {keycloakUser.Id}: {string.Join(", ", result.Errors.Select(error => error.Description))}"
                    );

                synced++;
            }

            if (users.Count() < PageSize)
                return synced;
        }
    }

    private static User MapKeycloakToUser(Guid userId, User? user, UserRepresentation keycloakUser)
    {
        var picture = keycloakUser.Attributes?.FirstOrDefault(x => x.Key == "picture").Value?.FirstOrDefault();
        var phoneNumber = keycloakUser.Attributes?.FirstOrDefault(x => x.Key == "phoneNumber").Value?.FirstOrDefault();
        var phoneNumberVerified = string.Equals(
            keycloakUser.Attributes?.FirstOrDefault(x => x.Key == "phoneNumberVerified").Value?.FirstOrDefault(),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        user ??= new User { FirstName = keycloakUser.FirstName ?? keycloakUser.Username ?? "", LastName = keycloakUser.LastName ?? "" };
        user.Id = userId;
        user.TwoFactorEnabled = keycloakUser.Totp ?? false;
        user.Email = keycloakUser.Email;
        user.EmailConfirmed = keycloakUser.EmailVerified ?? false;
        user.UserName = keycloakUser.Username ?? keycloakUser.Email ?? userId.ToString();
        user.FirstName = keycloakUser.FirstName ?? keycloakUser.Username ?? keycloakUser.Email ?? "";
        user.LastName = keycloakUser.LastName ?? "";
        user.Picture = picture;
        user.PhoneNumber = phoneNumber;
        user.PhoneNumberConfirmed = phoneNumberVerified;
        user.LastSync = DateTime.UtcNow;
        return user;
    }
}
