using Microsoft.Extensions.Caching.Hybrid;
using Club.Services;

namespace Club.Features.Payment.Result;

public class Endpoint(
    IPaymentFactory paymentFactory,
    HybridCache cache,
    ILogger<Endpoint> logger
) : EndpointWithoutRequest
{
    private readonly IPaymentFactory _paymentFactory = paymentFactory;
    private readonly HybridCache _cache = cache;
    private readonly ILogger<Endpoint> _logger = logger;

    public override void Configure()
    {
        Post("/payment/result/{provider}");
        Description(x => x.WithName("PaymentResultPost"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await PaymentResultHandler.HandleAsync(HttpContext, _paymentFactory, _cache, _logger, ct);
    }
}
