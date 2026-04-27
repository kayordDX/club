using Keycloak.AuthServices.Sdk.Admin;

namespace Online.Features.Test;

public class Endpoint(IKeycloakUserClient keycloakUserClient) : Endpoint<TestRequest, TestResponse>
{
    public override void Configure()
    {
        Get("/test");
        Description(x => x.WithName("Test"));
        // AllowAnonymous();
        // Policies(Constants.Policy.SuperAdmin);
        // Policies(Constants.Policy.OutletAdmin);
    }

    public override async Task HandleAsync(TestRequest req, CancellationToken ct)
    {
        var userCount = await keycloakUserClient.GetUserCountAsync("kayord", cancellationToken: ct);
        // var user = await keycloakUserClient.GetUserAsync("kayord", "ec35e6d3-d258-427a-a1e9-bc0ecab0aca2", cancellationToken: ct);

        // var accessToken = await HttpContext.GetTokenAsync(
        //     IdentityConstants.ApplicationScheme,
        //     "backchannel_access_token"
        // );

        // var users = await dbContext.Users.ToListAsync(ct);
        var response = new TestResponse
        {
            Success = true,
            Token = "test",
            // Other = user
        };
        await Send.OkAsync(response, ct);
    }
}
