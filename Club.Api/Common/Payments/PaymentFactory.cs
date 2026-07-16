using Club.Services;

namespace Club.Common.Payments;

public class PaymentFactory(IEnumerable<IPaymentProvider> providers) : IPaymentFactory
{
    private readonly IEnumerable<IPaymentProvider> _providers = providers;

    public IPaymentProvider GetProvider(string providerName)
    {
        return _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No payment provider is registered for '{providerName}'. " +
                $"Available providers: {string.Join(", ", _providers.Select(p => p.ProviderName))}");
    }
}
