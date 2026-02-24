using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.DTO;

namespace Online.Features.Outlet.Admin;

public class Endpoint(AppDbContext dbContext) : Endpoint<Request, OutletDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet/{slug}/admin");
        Description(x => x.WithName("OutletAdminGet"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
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
