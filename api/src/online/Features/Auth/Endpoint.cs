using System.Security.Claims;
using Online.Common;
using Online.Data;
using Online.Models;

namespace Online.Features.Auth;

public class Endpoint : EndpointWithoutRequest<Dictionary<string, string>?>
{

    public override void Configure()
    {
        Get("/me");
        Description(x => x.WithName("Me"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var claimsDictionary = HttpContext.User.Claims.ToDictionary(c => c.Type, c => c.Value);
        // var userId = Helpers.GetCurrentUserId(HttpContext);
        await Send.OkAsync(claimsDictionary, ct);
    }
}
