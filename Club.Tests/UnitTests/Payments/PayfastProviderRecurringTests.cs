using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Club.Common.Enums;
using Club.Common.Payments;
using Club.Common.Payments.Provider.Payfast;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Payments;

public class PayfastProviderRecurringTests
{
    private static readonly PayfastOptions SandboxOptions = new()
    {
        MerchantId = "10000100",
        MerchantKey = "46f0cd694581a",
        Passphrase = "jt7NOE43FZPn",
        BaseUrl = "https://sandbox.payfast.co.za/eng/process",
        ReturnUrl = "http://localhost:5173/payment/success",
        CancelUrl = "http://localhost:5173/payment/cancelled",
        NotifyUrl = "http://localhost:5000/payment/result/payfast",
    };

    private static PayfastProvider CreateProvider(HttpContext httpContext)
    {
        return new PayfastProvider(new FakeOptionsAccessor(SandboxOptions), new FakeHttpContextAccessor(httpContext), new HttpClient());
    }

    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost", 5000);
        return httpContext;
    }

    private static PaymentRequest CreateRequest(PaymentRecurring? recurring)
    {
        return new PaymentRequest
        {
            Amount = 150m,
            Currency = "ZAR",
            TransactionId = "txn-recurring-1",
            Description = "Booking #1",
            Recurring = recurring,
        };
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithRecurring_AppendsSubscriptionFields()
    {
        // Arrange
        var provider = CreateProvider(CreateHttpContext());
        var request = CreateRequest(
            new PaymentRecurring
            {
                Frequency = PaymentFrequencyEnum.Monthly,
                Cycles = 12,
                RecurringAmount = 99.50m,
                FirstBillingDate = new DateOnly(2026, 7, 1),
            }
        );

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - the subscription fields are added exactly as Payfast expects for recurring billing.
        Assert.True(response.Success);
        Assert.Equal("1", response.FormFields!["subscription_type"]);
        Assert.Equal("2026-07-01", response.FormFields["billing_date"]);
        Assert.Equal("99.50", response.FormFields["recurring_amount"]);
        Assert.Equal("3", response.FormFields["frequency"]);
        Assert.Equal("12", response.FormFields["cycles"]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithRecurring_SignatureCoversSubscriptionFields()
    {
        // Arrange
        var provider = CreateProvider(CreateHttpContext());
        var request = CreateRequest(
            new PaymentRecurring
            {
                Frequency = PaymentFrequencyEnum.Annually,
                Cycles = 0,
                RecurringAmount = 150m,
                FirstBillingDate = new DateOnly(2026, 1, 15),
            }
        );

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - recomputing the signature over every submitted field except the signature itself
        // must match, proving the subscription fields are part of the signed set (Payfast rejects an
        // unsigned recurring field).
        var formFields = response.FormFields!;
        var signed = formFields.Where(kvp => kvp.Key != "signature").Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value));
        var expected = ComputeSignature(signed, SandboxOptions.Passphrase);

        Assert.Equal(expected, formFields["signature"]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithRecurring_DefaultsRecurringAmountAndBillingDate()
    {
        // Arrange - omit the optional recurring amount and billing date.
        var provider = CreateProvider(CreateHttpContext());
        var request = CreateRequest(new PaymentRecurring { Frequency = PaymentFrequencyEnum.Quarterly, Cycles = 4 });

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - recurring amount falls back to the request amount and billing date to today.
        Assert.Equal("150.00", response.FormFields!["recurring_amount"]);
        Assert.Equal("4", response.FormFields["frequency"]);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), response.FormFields["billing_date"]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithoutRecurring_DoesNotAddSubscriptionFields()
    {
        // Arrange - a once-off payment (the existing behaviour) must be completely unaffected.
        var provider = CreateProvider(CreateHttpContext());
        var request = CreateRequest(recurring: null);

        // Act
        var response = await provider.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert - none of the recurring fields are present, guarding the working once-off flow.
        Assert.True(response.Success);
        Assert.False(response.FormFields!.ContainsKey("subscription_type"));
        Assert.False(response.FormFields.ContainsKey("billing_date"));
        Assert.False(response.FormFields.ContainsKey("recurring_amount"));
        Assert.False(response.FormFields.ContainsKey("frequency"));
        Assert.False(response.FormFields.ContainsKey("cycles"));
    }

    // Mirrors PayfastProvider.CalculateSignature so the test verifies the real signature independently.
    private static string ComputeSignature(IEnumerable<KeyValuePair<string, string>> parameters, string passphrase)
    {
        var paramString = string.Join("&", parameters.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).Select(kvp => $"{kvp.Key}={PayfastUrlEncode(kvp.Value)}"));
        var signatureString = string.IsNullOrEmpty(passphrase) ? paramString : $"{paramString}&passphrase={PayfastUrlEncode(passphrase)}";
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(signatureString));
        return Convert.ToHexStringLower(hashBytes);
    }

    private static string PayfastUrlEncode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var encoded = HttpUtility.UrlEncode(value);
        return Regex.Replace(encoded, @"%[0-9a-f]{2}", m => m.Value.ToUpperInvariant());
    }

    private sealed class FakeOptionsAccessor(PayfastOptions options) : IPaymentOptionsAccessor<PayfastOptions>
    {
        public Task<PayfastOptions?> GetAsync(CancellationToken ct) => Task.FromResult<PayfastOptions?>(options);
    }

    private sealed class FakeHttpContextAccessor(HttpContext httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }
}
