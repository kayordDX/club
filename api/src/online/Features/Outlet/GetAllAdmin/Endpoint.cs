using Online.Common.Extensions;
using Online.Common.Models;
using Online.Data;

namespace Online.Features.Outlet.GetAllAdmin;

public class Endpoint(AppDbContext dbContext) : Endpoint<Request, PaginatedList<Response>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet/admin");
        Description(x => x.WithName("OutletGetAllAdmin"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var results = await _dbContext.Outlet.Select(x => new Response { Id = x.Id }).OrderBy(x => x.Id).GetPagedAsync(req, ct);
        await Send.OkAsync(results, ct);
    }
}
