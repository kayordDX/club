using Club.Common.Extensions;
using Club.Common.Models;
using Club.Data;

namespace Club.Features.Outlet.GetAll;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletGetAllRequest, PaginatedList<Entities.Outlet>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet");
        Description(x => x.WithName("OutletGetAll"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(OutletGetAllRequest req, CancellationToken ct)
    {
        var results = await _dbContext.Outlet.OrderBy(x => x.Id).GetPagedAsync(req, ct);
        await Send.OkAsync(results, ct);
    }
}
