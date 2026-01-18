using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.What;

public class Endpoint(AppDbContext _dbContext) : Endpoint<string, string>
{
    public override void Configure()
    {
        Post("/what");
        Description(x => x.WithName("What"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(string r, CancellationToken ct)
    {
        var result = await _dbContext.UserClaims.ToListAsync(ct);
        await Send.OkAsync(result?.ToString() ?? "", ct);
    }
}
