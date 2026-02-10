using Online.Common.Extensions;
using Online.Common.Models;
using Online.Data;

namespace Online.Features.Outlet.GetAll;

public class Endpoint(AppDbContext dbContext) : Endpoint<Request, PaginatedList<Entities.Outlet>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet");
        Description(x => x.WithName("OutletGetAll"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var results = await _dbContext.Outlet.GetPagedAsync(req, ct);
        await Send.OkAsync(results, ct);
    }
}
