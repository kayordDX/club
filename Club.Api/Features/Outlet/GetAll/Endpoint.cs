using Club.Common.Extensions;
using Club.Common.Models;
using Club.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

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
        var query = _dbContext.Outlet.AsQueryable();

        // When a search term is supplied, filter using the PostgreSQL full-text search
        // vector (GIN indexed). The query is built inside the Where lambda so EF can
        // translate websearch_to_tsquery + the @@ operator to SQL (no client eval).
        // websearch_to_tsquery supports natural syntax like quoted phrases and is
        // forgiving of stray punctuation from a free-text box.
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            query = query.Where(o => o.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", req.Search)));
        }

        var results = await query.OrderBy(x => x.Id).GetPagedAsync(req, ct);
        await Send.OkAsync(results, ct);
    }
}
