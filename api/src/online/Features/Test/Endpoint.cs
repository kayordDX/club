using Keycloak.AuthServices.Sdk.Admin;
using Online.Common;

namespace Online.Features.Test;

public class Endpoint(IKeycloakUserClient keycloakUserClient) : Endpoint<TestRequest, TestResponse>
{
    public override void Configure()
    {
        Get("/test");
        Description(x => x.WithName("Test"));
        AllowAnonymous();
        // Policies(Constants.Policy.SuperAdmin);
        // Policies(Constants.Policy.OutletAdmin);
    }

    public override async Task HandleAsync(TestRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        // var userCount = await keycloakUserClient.GetUserCountAsync("kayord", cancellationToken: ct);
        var user = await keycloakUserClient.GetUserAsync("kayord", userId?.ToString() ?? "", cancellationToken: ct);

        // var accessToken = await HttpContext.GetTokenAsync(
        //     IdentityConstants.ApplicationScheme,
        //     "backchannel_access_token"
        // );

        // var users = await dbContext.Users.ToListAsync(ct);
        var response = new TestResponse
        {
            Success = true,
            Token = "test",
            Other = user
        };
        await Send.OkAsync(response, ct);
    }
}
