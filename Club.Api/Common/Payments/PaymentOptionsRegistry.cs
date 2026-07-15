using Club.Common.Payments.Provider.Payfast;
using Club.Common.Payments.Provider.Peach;
using Club.Entities;

namespace Club.Common.Payments;


public static class PaymentOptionsRegistry
{
    public static readonly IReadOnlyDictionary<string, Type> OptionsByProviderKey =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [PayfastOptions.Key] = typeof(PayfastOptions),
            [PeachOptions.Key] = typeof(PeachOptions),
        };

    private static readonly IReadOnlyDictionary<string, string> ProviderNameByKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PayfastOptions.Key] = "payfast",
            [PeachOptions.Key] = "peach",
        };

    public static Type GetOptionsType(string providerKey)
    {
        return OptionsByProviderKey.TryGetValue(providerKey, out var type)
            ? type
            : throw new InvalidOperationException(
                $"No payment options type is registered for provider key '{providerKey}'. " +
                $"Known keys: {string.Join(", ", OptionsByProviderKey.Keys)}.");
    }

    public static string GetProviderName(PaymentProviderType type)
    {
        return ProviderNameByKey.TryGetValue(type.GetKey(), out var name)
            ? name
            : throw new InvalidOperationException(
                $"No provider name is registered for type '{type}'. " +
                $"Known keys: {string.Join(", ", ProviderNameByKey.Keys)}.");
    }
}
