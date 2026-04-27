using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Online.Common;

namespace Online.Features.Account.Session;

public class Endpoint(KeycloakAdminApiClient keycloakAdminClient) : EndpointWithoutRequest<List<AccountSessionResponse>>
{
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

        var clients = await keycloakAdminClient.Admin.Realms["kayord"].Clients.GetAsync(config =>
        {
            config.QueryParameters.ClientId = "public-client";
        }, cancellationToken: ct);

        var client = clients?.FirstOrDefault();

        if (client == null || client.Id == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var sessions = await keycloakAdminClient.Admin.Realms["kayord"]
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
