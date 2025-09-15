using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Online.Common;
using Online.Data;

namespace Online.Features.Account.Me;

public class Endpoint : EndpointWithoutRequest<List<Response>>
{
    private readonly AppDbContext _dbContext;

    public Endpoint(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/account/me");
        Description(x => x.WithName("AccountMe"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);

        var users = await _dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new Response()
            {
                Email = x.Email ?? string.Empty,
                FirstName = x.FirstName,
                LastName = x.LastName
            }).ToListAsync(ct);
        await Send.OkAsync(users);
    }
}