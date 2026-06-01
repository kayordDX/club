using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Microsoft.Extensions.Options;
using Online.Common;
using Online.Common.Config;

namespace Online.Features.Account.Session.Revoke;

public class Endpoint(KeycloakAdminApiClient keycloakAdminClient, IOptions<KeycloakConfig> keycloakConfig) : Endpoint<AccountSessionRevokeRequest>
{
    private readonly KeycloakConfig keycloakConfig = keycloakConfig.Value;

    public override void Configure()
    {
        Post("/account/session/revoke");
        Description(x => x.WithName("AccountSessionRevoke"));
    }

    public override async Task HandleAsync(AccountSessionRevokeRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await keycloakAdminClient.Admin.Realms[keycloakConfig.Realm]
            .Sessions[req.Id]
            .DeleteAsync(o => o.QueryParameters.IsOffline = true, cancellationToken: ct);

        await Send.NoContentAsync(ct);
    }
}
