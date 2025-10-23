using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Entities;

namespace Online.Features.Test;

public class Endpoint : Endpoint<Request, List<User>>
{
    private readonly AppDbContext _dbContext;

    public Endpoint(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/test");
        Description(x => x.WithName("Test"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        await Task.Delay(2000, ct);
        var users = await _dbContext.Users.ToListAsync(ct);
        await Send.OkAsync(users);
    }
}