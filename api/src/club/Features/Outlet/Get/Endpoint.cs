using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.DTO;

namespace Online.Features.Outlet.Get;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletGetRequest, OutletDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet/{slug}");
        Description(x => x.WithName("OutletGet"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(OutletGetRequest req, CancellationToken ct)
    {
        var results = await _dbContext.Outlet.ProjectToDto().FirstOrDefaultAsync(x => x.Slug == req.Slug, ct);
        if (results == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(results, ct);
    }
}
