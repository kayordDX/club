using Microsoft.EntityFrameworkCore;
using Online.Common;
using Online.Data;
using Online.Entities;

namespace Online.Features.Test;

public class Endpoint(AppDbContext dbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("/test");
        Description(x => x.WithName("Test"));
        // Policies(Constants.Policy.SuperAdmin);
        Policies(Constants.Policy.OutletAdmin);
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var users = await dbContext.Users.ToListAsync(ct);
        var response = new Response
        {
            Success = true
        };
        await Send.OkAsync(response, ct);
    }
}
