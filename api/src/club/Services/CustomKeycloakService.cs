using Keycloak.AuthServices.Sdk.Admin;
using Microsoft.Extensions.Options;
using Online.Common.Config;

namespace Online.Services;

public interface ICustomKeycloakService
{
    Task<bool> DisableUserTotpAsync(Guid userId, CancellationToken ct);
}

public class CustomKeycloakService(
    IKeycloakUserClient keycloakUserClient,
    IOptions<KeycloakConfig> keycloakConfig) : ICustomKeycloakService
{
    private readonly IKeycloakUserClient _keycloakUserClient = keycloakUserClient;
    private readonly KeycloakConfig _config = keycloakConfig.Value;

    public async Task<bool> DisableUserTotpAsync(Guid userId, CancellationToken ct)
    {
        var credentials = await _keycloakUserClient.GetCredentialsAsync(
            _config.Realm, userId.ToString(), cancellationToken: ct);

        var otpCredential = credentials?.FirstOrDefault(c => c.Type == "otp");
        if (otpCredential?.Id is null)
            return false;

        await _keycloakUserClient.DeleteCredentialAsync(
            _config.Realm, userId.ToString(), otpCredential.Id, cancellationToken: ct);

        return true;
    }
}
