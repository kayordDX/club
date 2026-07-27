namespace Club.Features.Payment.Form;

public class Endpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/payment/form/{provider}/{transactionId}");
        Description(x => x.WithName("PaymentForm"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await PaymentFormHandler.HandleAsync(HttpContext, ct);
    }
}
