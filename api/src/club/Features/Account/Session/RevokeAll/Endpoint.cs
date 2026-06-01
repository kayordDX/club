using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Microsoft.Extensions.Options;
using Online.Common;
using Online.Common.Config;

namespace Online.Features.Account.Session.RevokeAll;

public class Endpoint(KeycloakAdminApiClient keycloakAdminClient, IOptions<KeycloakConfig> keycloakConfig) : EndpointWithoutRequest
{
    private readonly KeycloakConfig keycloakConfig = keycloakConfig.Value;

    public override void Configure()
    {
        Post("/account/session/revokeAll");
        Description(x => x.WithName("AccountSessionRevokeAll"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var clients = await keycloakAdminClient.Admin.Realms[keycloakConfig.Realm].Clients.GetAsync(config =>
        {
            config.QueryParameters.ClientId = keycloakConfig.PublicClientId;
        }, cancellationToken: ct);

        var client = clients?.FirstOrDefault();

        if (client == null || client.Id == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var sessions = await keycloakAdminClient.Admin.Realms[keycloakConfig.Realm]
            .Users[userId.ToString()]
            .OfflineSessions[client.Id.ToString()]
            .GetAsync(cancellationToken: ct) ?? [];

        foreach (var session in sessions)
        {
            await keycloakAdminClient.Admin.Realms[keycloakConfig.Realm]
                .Sessions[session.Id]
                .DeleteAsync(o => o.QueryParameters.IsOffline = true, cancellationToken: ct);
        }

        await keycloakAdminClient.Admin.Realms[keycloakConfig.Realm]
            .Users[userId.ToString()]
            .Logout.PostAsync(cancellationToken: ct);

        await Send.NoContentAsync(ct);
    }
}
