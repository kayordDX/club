using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Online.Common;

namespace Online.Features.Account.Session.Revoke;

public class Endpoint(KeycloakAdminApiClient keycloakAdminClient) : Endpoint<AccountSessionRevokeRequest>
{
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

        await keycloakAdminClient.Admin.Realms["kayord"]
            .Sessions[req.Id]
            .DeleteAsync(o => o.QueryParameters.IsOffline = true, cancellationToken: ct);

        await Send.NoContentAsync(ct);
    }
}
