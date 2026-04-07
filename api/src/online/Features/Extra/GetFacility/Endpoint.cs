using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.Extra.GetFacility;

public class Endpoint(AppDbContext dbContext) : Endpoint<ExtraGetFacilityRequest, List<Entities.Extra>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/extra/facility/{FacilityId}");
        Description(x => x.WithName("ExtraGetFacility"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(ExtraGetFacilityRequest req, CancellationToken ct)
    {
        var results = await _dbContext.Extra.Where(x => x.FacilityId == req.FacilityId).ToListAsync(ct);
        await Send.OkAsync(results, ct);
    }
}
