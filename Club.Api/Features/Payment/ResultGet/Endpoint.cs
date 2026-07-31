using Club.Features.Payment.Result;
using Club.Services;

namespace Club.Features.Payment.ResultGet;

public class Endpoint(IPaymentFactory paymentFactory, ILogger<Endpoint> logger) : EndpointWithoutRequest
{
    private readonly IPaymentFactory _paymentFactory = paymentFactory;
    private readonly ILogger<Endpoint> _logger = logger;

    public override void Configure()
    {
        Get("/payment/result/{provider}");
        Description(x => x.WithName("PaymentResultGet"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await PaymentResultHandler.HandleAsync(HttpContext, _paymentFactory, _logger, ct);
    }
}
