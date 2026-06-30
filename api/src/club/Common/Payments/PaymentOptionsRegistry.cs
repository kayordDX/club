using Club.Common.Payments.Provider.Payfast;
using Club.Common.Payments.Provider.Peach;

namespace Club.Common.Payments;


public static class PaymentOptionsRegistry
{
    public static readonly IReadOnlyDictionary<string, Type> OptionsByProviderKey =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [PayfastOptions.Key] = typeof(PayfastOptions),
            [PeachOptions.Key] = typeof(PeachOptions),
        };

    public static Type GetOptionsType(string providerKey)
    {
        return OptionsByProviderKey.TryGetValue(providerKey, out var type)
            ? type
            : throw new InvalidOperationException(
                $"No payment options type is registered for provider key '{providerKey}'. " +
                $"Known keys: {string.Join(", ", OptionsByProviderKey.Keys)}.");
    }
}
