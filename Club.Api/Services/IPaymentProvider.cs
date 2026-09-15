using Club.Common.Payments;

namespace Club.Services;

public interface IPaymentProvider
{
    string ProviderName { get; }

    // Whether this provider can set up a recurring subscription from PaymentRequest.Recurring.
    // Providers that return false must be rejected before processing when recurring is requested,
    // so a payer is never silently charged once-off while believing they created a subscription.
    bool SupportsRecurring { get; }

    Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct);
    Task<PaymentResult> ProcessResponseAsync(HttpContext context);
}
