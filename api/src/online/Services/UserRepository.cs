using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Entities;

namespace Online.Services;

public class UserRepository
{
    private readonly AppDbContext _appDbContext;

    public UserRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        var user = await _appDbContext.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        return user;
    }
}