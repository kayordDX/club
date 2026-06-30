using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Club.Common.Payments.Provider.Peach;
using Club.Common.Payments.Provider.Payfast;
using Club.Data;
using Club.Entities;
using Club.Services;

namespace Club.Common.Payments;

public static class PaymentProviderConfigSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        EncryptionService encryption,
        IOptions<PeachOptions> peachOptions,
        IOptions<PayfastOptions> payfastOptions,
        CancellationToken ct)
    {
        await EnsureRowAsync(db, encryption, PayfastOptions.Key, payfastOptions.Value, ct);
        await EnsureRowAsync(db, encryption, PeachOptions.Key, peachOptions.Value, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureRowAsync<T>(
        AppDbContext db,
        EncryptionService encryption,
        string providerKey,
        T options,
        CancellationToken ct) where T : class
    {
        var exists = await db.PaymentProviderConfig
            .AnyAsync(c => c.ProviderKey == providerKey, ct);

        if (exists)
        {
            return;
        }

        var json = JsonSerializer.Serialize(options);
        var iv = encryption.GenerateIV();
        var encrypted = encryption.Encrypt(json, iv);

        db.PaymentProviderConfig.Add(new PaymentProviderConfig
        {
            ProviderKey = providerKey,
            Iv = iv,
            EncryptedSettings = encrypted,
            Enabled = true,
        });
    }
}
