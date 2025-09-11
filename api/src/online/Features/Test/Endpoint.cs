
namespace Online.Features.Test;

public class Endpoint : EndpointWithoutRequest<bool>
{
    private readonly ILogger<Endpoint> _logger;
    public Endpoint(ILogger<Endpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/test");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("Test endpoint hit");
        await Send.OkAsync(true);
    }
}