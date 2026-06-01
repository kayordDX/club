using Keycloak.AuthServices.Sdk.Admin;
using Online.Common;

namespace Online.Features.Account.Credential;

public class Endpoint(IKeycloakUserClient keycloakUserClient) : EndpointWithoutRequest<AccountCredentialResponse>
{
    public override void Configure()
    {
        Get("/account/credential");
        Description(x => x.WithName("AccountCredential"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var credentials = await keycloakUserClient.GetCredentialsAsync("kayord", userId.ToString() ?? "", cancellationToken: ct);

        var response = new AccountCredentialResponse
        {
            IsTwoFactorEnabled = credentials.Any(c => c.Type == "otp"),
            HasCredential = credentials.Any()
        };

        await Send.OkAsync(response, ct);
    }
}
