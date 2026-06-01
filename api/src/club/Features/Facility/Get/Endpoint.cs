using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.DTO;

namespace Club.Features.Facility.Get;

public class Endpoint(AppDbContext dbContext) : Endpoint<FacilityGetRequest, FacilityDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/facility/{id}");
        Description(x => x.WithName("FacilityGet"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(FacilityGetRequest req, CancellationToken ct)
    {
        var results = await _dbContext.Facility.ProjectToDto().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (results == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(results, ct);
    }
}
