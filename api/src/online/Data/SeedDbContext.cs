using Microsoft.EntityFrameworkCore;
using Online.Entities;

namespace Online.Data;

public static class SeedDbContext
{
    public static async Task SeedData(AppDbContext dbContext, CancellationToken ct)
    {
        // Temp drop all for now
        // await db.Database.ExecuteSqlAsync($"""
        //     DROP SCHEMA public CASCADE;
        //     CREATE SCHEMA public;
        //     GRANT ALL ON SCHEMA public TO online;
        // """, ct);

        await dbContext.Business.ExecuteDeleteAsync(ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE facility_id_seq RESTART WITH 1;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from facility_type;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE facility_type_id_seq RESTART WITH 1;", ct);


        if (!dbContext.Business.Any())
        {
            var business = new Business { Name = "Business" };
            var outletType = new OutletType { Name = "Golf Course" };
            var outlet = new Outlet { Name = "Ruimsig Country Club", Slug = "ruimsig-country-club", DisplayName = "RCC", Business = business, OutletType = outletType, VatNumber = "1234567", IsActive = true };
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
