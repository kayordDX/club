using Club.Common.Payments;

namespace Club.Services;

public interface IPaymentProvider
{
    string ProviderName { get; }

    Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct);
    Task<PaymentResult> ProcessResponseAsync(HttpContext context);
}
