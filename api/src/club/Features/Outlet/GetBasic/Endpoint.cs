using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.DTO;

namespace Club.Features.Outlet.GetBasic;

public class Endpoint(AppDbContext dbContext) : Endpoint<OutletGetBasicRequest, OutletBasicDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/outlet/basic/{slug}");
        Description(x => x.WithName("OutletGetBasic"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(OutletGetBasicRequest req, CancellationToken ct)
    {
        var results = await _dbContext.Outlet
            .Select(x => new OutletBasicDTO
            {
                Id = x.Id,
                Slug = x.Slug,
                Name = x.Name,
                BusinessName = x.Business.Name,
                Logo = x.Logo,
                DisplayName = x.DisplayName,
                OutletTypeId = x.OutletTypeId,
                OutletType = new OutletTypeDTO
                {
                    Id = x.OutletType.Id,
                    Name = x.OutletType.Name
                },
                IsActive = x.IsActive,
                Facilities = x.Facilities.Select(f => new FacilityBasicDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsActive = f.IsActive,
                    FacilityTypeId = f.FacilityTypeId,
                    FacilityTypeName = f.FacilityType.Name
                }).ToList()
            })
            .FirstOrDefaultAsync(x => x.Slug == req.Slug, ct);

        if (results == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(results, ct);
    }
}
