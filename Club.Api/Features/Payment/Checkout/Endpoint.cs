using Club.Services;

namespace Club.Features.Payment.Checkout;

public class Endpoint(IPaymentFactory paymentFactory) : Endpoint<PaymentCheckoutRequest, PaymentCheckoutResponse>
{
    private readonly IPaymentFactory _paymentFactory = paymentFactory;

    public override void Configure()
    {
        Post("/payment/checkout/{provider}");
        Description(x => x.WithName("PaymentCheckout"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(PaymentCheckoutRequest req, CancellationToken ct)
    {
        IPaymentProvider provider;
        try
        {
            provider = _paymentFactory.GetProvider(req.Provider);
        }
        catch (InvalidOperationException ex)
        {
            AddError(x => x.Provider, ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var paymentRequest = new Common.Payments.PaymentRequest
        {
            Amount = req.Amount,
            Currency = req.Currency,
            TransactionId = req.TransactionId,
            Description = req.Description ?? $"Club payment {req.TransactionId}"
        };

        var result = await provider.ProcessPaymentAsync(paymentRequest, ct);

        await Send.OkAsync(new PaymentCheckoutResponse
        {
            Success = result.Success,
            TransactionId = result.TransactionId,
            ProviderReference = result.ProviderReference,
            RedirectUrl = result.RedirectUrl,
            Status = result.Status,
            ErrorMessage = result.ErrorMessage
        }, ct);
    }
}
