
using Microsoft.EntityFrameworkCore;
using Online.Data;

namespace Online.Features.Test;

public class Endpoint : EndpointWithoutRequest<List<int>>
{
    private readonly ILogger<Endpoint> _logger;
    private readonly AppDbContext _dbContext;
    public Endpoint(ILogger<Endpoint> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/test");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("Test endpoint hit");
        var result = await _dbContext.Users.Select(x => x.Id).ToListAsync(ct);
        await Send.OkAsync(result);
    }
}