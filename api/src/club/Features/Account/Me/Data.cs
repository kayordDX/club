using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Models;

namespace Online.Features.Account.Me;

public static class Data
{
    public static async Task<UserModel?> Get(Guid? userId, AppDbContext _dbContext, CancellationToken ct)
    {
        var user = await _dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new UserModel()
            {
                Id = x.Id.ToString(),
                Email = x.Email ?? string.Empty,
                Name = x.FirstName + " " + x.LastName,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Picture = x.Picture,
                EmailConfirmed = x.EmailConfirmed
            }).FirstOrDefaultAsync(ct);
        return user;
    }
}
