using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Microsoft.EntityFrameworkCore;
using Online.Common;
using Online.Data;
using Online.Services;

namespace Online.Features.Account.Session.RevokeAll;

public class Endpoint(KeycloakAdminApiClient keycloakAdminClient) : EndpointWithoutRequest
{
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

        await keycloakAdminClient.Admin.Realms["kayord"]
            .Users[userId.ToString()]
            .Logout.PostAsync(cancellationToken: ct);

        await Send.NoContentAsync(ct);
    }
}
