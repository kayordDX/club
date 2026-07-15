using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Club.Data;
using Club.Services;

namespace Club.Common.Payments;

public class PaymentOptionsAccessor<T>(
    AppDbContext dbContext,
    EncryptionService encryption,
    HybridCache cache) : IPaymentOptionsAccessor<T> where T : class
{
    private static readonly string ProviderKey = ResolveKey();

    private readonly AppDbContext _dbContext = dbContext;
    private readonly EncryptionService _encryption = encryption;
    private readonly HybridCache _cache = cache;

    public async Task<T?> GetAsync(CancellationToken ct)
    {
        var row = await _dbContext.PaymentProviderConfig
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ProviderKey == ProviderKey && c.Enabled, ct);

        if (row is null)
        {
            return null;
        }

        return await _cache.GetOrCreateAsync(
            CacheKey(ProviderKey),
            async _ => Decrypt(row.Iv, row.EncryptedSettings),
            cancellationToken: ct);
    }

    private T Decrypt(byte[] iv, string encryptedSettings)
    {
        var json = _encryption.Decrypt(encryptedSettings, iv);
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize {typeof(T).Name} from stored payment provider config.");
    }

    public static string CacheKey(string providerKey) => $"payment-options:{providerKey}";

    private static string ResolveKey()
    {
        return PaymentOptionsRegistry.OptionsByProviderKey
            .FirstOrDefault(kv => kv.Value == typeof(T)).Key
            ?? throw new InvalidOperationException(
                $"{typeof(T).Name} is not registered in {nameof(PaymentOptionsRegistry)}. " +
                "Add it before resolving options for that provider.");
    }
}
