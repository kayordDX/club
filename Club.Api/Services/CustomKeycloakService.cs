using Club.Common.Config;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using Microsoft.Extensions.Options;

namespace Club.Services;

public record KeycloakUserResult(Guid Id, string? FirstName, string? LastName, string? Email, string? PhoneNumber);

public interface ICustomKeycloakService
{
    Task<bool> DisableUserTotpAsync(Guid userId, CancellationToken ct);

    // Search Keycloak for a single user by an email, username or phone number. Returns null when no
    // match is found.
    Task<KeycloakUserResult?> FindUserAsync(string query, CancellationToken ct);

    // Create a new Keycloak user and trigger the account-setup email so the new member sets their own
    // password through Keycloak (the register flow). Returns the new user's id.
    Task<Guid> CreateUserAsync(string email, string firstName, string lastName, string? phoneNumber, CancellationToken ct);

    // Fetch a single Keycloak user by id (used to mirror them into the local user table).
    Task<KeycloakUserResult?> GetUserAsync(Guid userId, CancellationToken ct);
}

public class CustomKeycloakService(IKeycloakUserClient keycloakUserClient, IOptions<KeycloakConfig> keycloakConfig) : ICustomKeycloakService
{
    private readonly IKeycloakUserClient _keycloakUserClient = keycloakUserClient;
    private readonly KeycloakConfig _config = keycloakConfig.Value;

    public async Task<bool> DisableUserTotpAsync(Guid userId, CancellationToken ct)
    {
        var credentials = await _keycloakUserClient.GetCredentialsAsync(_config.Realm, userId.ToString(), cancellationToken: ct);

        var otpCredential = credentials?.FirstOrDefault(c => c.Type == "otp");
        if (otpCredential?.Id is null)
            return false;

        await _keycloakUserClient.DeleteCredentialAsync(_config.Realm, userId.ToString(), otpCredential.Id, cancellationToken: ct);

        return true;
    }

    public async Task<KeycloakUserResult?> FindUserAsync(string query, CancellationToken ct)
    {
        var trimmed = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        // Keycloak's `search` matches username, email, first and last name. Phone numbers are stored
        // as a custom attribute, so also try an explicit attribute search when nothing matches.
        var users = await _keycloakUserClient.GetUsersAsync(_config.Realm, new GetUsersRequestParameters { Search = trimmed, Max = 20 }, cancellationToken: ct);

        var match = users?.FirstOrDefault(u => Matches(u, trimmed));

        if (match is null)
        {
            var byAttribute = await _keycloakUserClient.GetUsersAsync(
                _config.Realm,
                new GetUsersRequestParameters { Query = $"phoneNumber:{trimmed}", Max = 20 },
                cancellationToken: ct
            );
            match = byAttribute?.FirstOrDefault();
        }

        if (match?.Id is null || !Guid.TryParse(match.Id, out var id))
        {
            return null;
        }

        var phone = match.Attributes?.FirstOrDefault(a => a.Key == "phoneNumber").Value?.FirstOrDefault();
        return new KeycloakUserResult(id, match.FirstName, match.LastName, match.Email, phone);
    }

    public async Task<Guid> CreateUserAsync(string email, string firstName, string lastName, string? phoneNumber, CancellationToken ct)
    {
        var representation = new UserRepresentation
        {
            Email = email,
            Username = email,
            FirstName = firstName,
            LastName = lastName,
            Enabled = true,
            EmailVerified = false,
        };

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            representation.Attributes = new Dictionary<string, ICollection<string>> { ["phoneNumber"] = [phoneNumber] };
        }

        var response = await _keycloakUserClient.CreateUserWithResponseAsync(_config.Realm, representation, ct);
        response.EnsureSuccessStatusCode();

        // Keycloak returns the new user's id in the Location header (…/users/{id}).
        var location = response.Headers.Location?.ToString();
        var idSegment = location?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        if (idSegment is null || !Guid.TryParse(idSegment, out var userId))
        {
            // Fall back to resolving the freshly created user by email.
            var created = await _keycloakUserClient.GetUsersAsync(
                _config.Realm,
                new GetUsersRequestParameters { Email = email, Exact = true },
                cancellationToken: ct
            );
            var createdId = created?.FirstOrDefault()?.Id;
            if (createdId is null || !Guid.TryParse(createdId, out userId))
            {
                throw new InvalidOperationException("Could not resolve the id of the newly created Keycloak user.");
            }
        }

        // Send the account-setup email: the new member sets their own password and verifies their
        // email through Keycloak's hosted pages (the register flow).
        await _keycloakUserClient.ExecuteActionsEmailAsync(
            _config.Realm,
            userId.ToString(),
            new ExecuteActionsEmailRequest { Actions = ["UPDATE_PASSWORD", "VERIFY_EMAIL"] },
            cancellationToken: ct
        );

        return userId;
    }

    public async Task<KeycloakUserResult?> GetUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _keycloakUserClient.GetUserAsync(_config.Realm, userId.ToString(), cancellationToken: ct);
        if (user?.Id is null || !Guid.TryParse(user.Id, out var id))
        {
            return null;
        }

        var phone = user.Attributes?.FirstOrDefault(a => a.Key == "phoneNumber").Value?.FirstOrDefault();
        return new KeycloakUserResult(id, user.FirstName, user.LastName, user.Email, phone);
    }

    private static bool Matches(UserRepresentation user, string query)
    {
        var phone = user.Attributes?.FirstOrDefault(a => a.Key == "phoneNumber").Value?.FirstOrDefault();
        return string.Equals(user.Email, query, StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Username, query, StringComparison.OrdinalIgnoreCase)
            || (phone is not null && string.Equals(phone, query, StringComparison.OrdinalIgnoreCase));
    }
}
