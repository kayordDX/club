using Microsoft.EntityFrameworkCore;
using Online.Entities;

namespace Online.Data;

public static class SeedDbContext
{
    public static async Task SeedData(AppDbContext dbContext, CancellationToken ct)
    {
        if (!dbContext.Business.Any())
        {
            var business = new Business { Name = "Business" };
            var outletType = new OutletType { Name = "Golf Course" };
            var outlet = new Outlet { Name = "Ruimsig Country Club", DisplayName = "RCC", Business = business, OutletType = outletType, VatNumber = "1234567" };
            await dbContext.Outlet.AddAsync(outlet, ct);
            var facilityTypeGolf = new FacilityType { Name = "Golf Course" };
            var facilityTypePaddle = new FacilityType { Name = "Paddle" };
            var facility1 = new Facility { Name = "Ruimsig Golf Course", IsActive = true, OutletId = 1, FacilityType = facilityTypeGolf, Outlet = outlet };
            var facility2 = new Facility { Name = "Ruimsig Paddle Court", IsActive = true, OutletId = 1, FacilityType = facilityTypePaddle, Outlet = outlet };
            await dbContext.Facility.AddAsync(facility1, ct);
            await dbContext.Facility.AddAsync(facility2, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        // if(!dbContext)
    }
}
