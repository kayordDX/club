using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Microsoft.Extensions.Options;
using Online.Common;
using Online.Common.Config;

namespace Online.Features.Account.Session;

public class Endpoint(KeycloakAdminApiClient keycloakAdminClient, IOptions<KeycloakConfig> keycloakConfig) : EndpointWithoutRequest<List<AccountSessionResponse>>
{
    private readonly KeycloakConfig keycloakConfig = keycloakConfig.Value;

    public override void Configure()
    {
        Get("/account/session");
        Description(x => x.WithName("AccountSession"));
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

        var response = sessions
            .Select(session => new AccountSessionResponse(session))
            .OrderByDescending(s => s.LastAccess)
            .ToList();

        await Send.OkAsync(response, ct);
    }
}
