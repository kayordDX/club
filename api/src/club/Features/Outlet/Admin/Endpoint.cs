using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.DTO;

namespace Club.Features.Outlet.Admin;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletAdminGetRequest, OutletDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet/{slug}/admin");
        Description(x => x.WithName("OutletAdminGet"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(OutletAdminGetRequest req, CancellationToken ct)
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
