using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Online.Common;
using Online.Data;
using Online.Entities;

namespace Online.Features.Account.Session;

public class Endpoint(KeycloakAdminApiClient keycloakAdminClient, IKeycloakUserClient keycloakUserClient) : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/account/session");
        Description(x => x.WithName("AccountSession"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // var userCount = await keycloakUserClient.GetUserCountAsync("kayord", cancellationToken: ct);
        var userId = Helpers.GetCurrentUserId(HttpContext);

        Console.WriteLine($"User ID: {userId}");

        // var user = await keycloakAdminClient.Admin.Realms["kayord"].Users["4048f991-681a-441b-b4c7-bd84d41c2b9a"]
        // .Sessions
        // .GetAsync(cancellationToken: ct);

        var clients = await keycloakAdminClient.Admin.Realms["kayord"].Clients.GetAsync(config =>
        {
            config.QueryParameters.ClientId = "public-client";
        });

        var client = clients.ToList().FirstOrDefault();

        // var count = keycloakAdminClient.Admin.Realms["kayord"]
        //     .Users["4048f991-681a-441b-b4c7-bd84d41c2b9a"]
        //     .OfflineSessions["public-client"].GetAsync(cancellationToken: ct);

        var sessions = await keycloakAdminClient.Admin.Realms["kayord"]
            .Users["4048f991-681a-441b-b4c7-bd84d41c2b9a"]
            .OfflineSessions[client.Id.ToString()]
            .GetAsync(cancellationToken: ct);

        // var aaaa = await keycloakAdminClient.Admin.Realms["kayord"].Clients["public-client"].UserSessions
        //     .GetAsync(cancellationToken: ct);

        // var sessions = await keycloakAdminClient.Get ("kayord", "4048f991-681a-441b-b4c7-bd84d41c2b9a", cancellationToken: ct);

        // var user = await keycloakUserClient.GetUserAsync("kayord", "ec35e6d3-d258-427a-a1e9-bc0ecab0aca2", cancellationToken: ct);

        // var accessToken = await HttpContext.GetTokenAsync(
        //     IdentityConstants.ApplicationScheme,
        //     "backchannel_access_token"
        // );

        var userCount = await keycloakUserClient.GetUserCountAsync("kayord", cancellationToken: ct);

        // var users = await dbContext.Users.ToListAsync(ct);
        await Send.OkAsync(new { sessions, userCount }, ct);
    }
}
