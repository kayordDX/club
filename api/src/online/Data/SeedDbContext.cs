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
            await dbContext.Outlet.AddAsync(new Outlet { Name = "Ruimsig Country Club", DisplayName = "RCC", Business = business, OutletType = outletType, VatNumber = "1234567" }, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
