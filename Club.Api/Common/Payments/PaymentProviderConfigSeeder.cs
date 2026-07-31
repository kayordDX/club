using System.Text.Json;
using Club.Common.Payments.Provider.Payfast;
using Club.Common.Payments.Provider.Peach;
using Club.Data;
using Club.Entities;
using Club.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Club.Common.Payments;

public static class PaymentProviderConfigSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        EncryptionService encryption,
        IOptions<PeachOptions> peachOptions,
        IOptions<PayfastOptions> payfastOptions,
        CancellationToken ct
    )
    {
        var facility = await db.Facility.FirstOrDefaultAsync(ct);

        if (facility is null)
        {
            return;
        }

        var payfast = string.IsNullOrEmpty(payfastOptions.Value.MerchantId)
            ? new PayfastOptions
            {
                MerchantId = "10016644",
                MerchantKey = "g9xjpawq6f6pr",
                Passphrase = "jt7NOE43FZPn",
                BaseUrl = "https://sandbox.payfast.co.za/eng/process",
                ReturnUrl = "http://localhost:5173/payment/success",
                CancelUrl = "http://localhost:5173/payment/cancelled",
                NotifyUrl = "http://localhost:5000/payment/result/payfast",
            }
            : payfastOptions.Value;

        await EnsureRowAsync(db, encryption, facility.Id, PayfastOptions.Key, PaymentProviderType.Payfast, payfast, ct);

        var peach = string.IsNullOrEmpty(peachOptions.Value.EntityId)
            ? new PeachOptions
            {
                EntityId = "8ac7a4c894809722019482d1df62029d",
                UserId = "5fb5e392d6fa11ef9b3002f694e28f55",
                Password = "OMydSc7ewVmEKPZCAj2WxHoik",
                BaseUrl = "https://testapi-v2.peachpayments.com",
            }
            : peachOptions.Value;

        await EnsureRowAsync(db, encryption, facility.Id, PeachOptions.Key, PaymentProviderType.Peach, peach, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureRowAsync<T>(
        AppDbContext db,
        EncryptionService encryption,
        int facilityId,
        string providerKey,
        PaymentProviderType type,
        T options,
        CancellationToken ct
    )
        where T : class
    {
        var exists = await db.PaymentProviderConfig.AnyAsync(c => c.FacilityId == facilityId && c.ProviderKey == providerKey, ct);

        if (exists)
        {
            return;
        }

        var json = JsonSerializer.Serialize(options);
        var iv = encryption.GenerateIV();
        var encrypted = encryption.Encrypt(json, iv);

        db.PaymentProviderConfig.Add(
            new PaymentProviderConfig
            {
                FacilityId = facilityId,
                ProviderKey = providerKey,
                Type = type,
                Iv = iv,
                EncryptedSettings = encrypted,
                Enabled = true,
            }
        );
    }
}
