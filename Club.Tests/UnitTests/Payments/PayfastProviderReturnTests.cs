using System.Security.Cryptography;
using System.Text;
using Club.Common.Payments;
using Club.Common.Payments.Provider.Payfast;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace UnitTests.Payments;

public class PayfastProviderReturnTests
{
    private static readonly PayfastOptions Options = new()
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
        return new PayfastProvider(new FakeOptionsAccessor(Options), new FakeHttpContextAccessor(httpContext), new HttpClient());
    }

    [Fact]
    public async Task ProcessPaymentAsync_RoutesReturnThroughBackendResultEndpoint()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost", 5000);
        var provider = CreateProvider(httpContext);

        // Act
        var response = await provider.ProcessPaymentAsync(
            new PaymentRequest
            {
                Amount = 100m,
                Currency = "ZAR",
                TransactionId = "txn-123",
                Description = "Booking #1",
            },
            CancellationToken.None
        );

        // Assert - the browser return must hit the API result endpoint (which verifies the signed
        // fields and marks the booking paid) instead of a page that never syncs the booking.
        Assert.True(response.Success);
        Assert.Equal("http://localhost:5000/payment/result/payfast", response.FormFields!["return_url"]);
        Assert.Equal("txn-123", response.FormFields["m_payment_id"]);
        Assert.Equal(Options.NotifyUrl, response.FormFields["notify_url"]);
        Assert.Equal(Options.CancelUrl, response.FormFields["cancel_url"]);
    }

    [Fact]
    public async Task ProcessResponseAsync_GetReturnWithValidSignatureAndCompleteStatus_SucceedsWithoutServerValidation()
    {
        // Arrange - a signed browser return exactly as PayFast appends it to the return_url.
        // (Fixture values are URL-safe ASCII so PayfastUrlEncode is the identity transform.)
        var fields = new Dictionary<string, string>
        {
            ["m_payment_id"] = "txn-123",
            ["pf_payment_id"] = "pf-456",
            ["payment_status"] = "COMPLETE",
            ["amount_gross"] = "100.00",
        };
        fields["signature"] = ComputeSignature(fields, Options.Passphrase);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.QueryString = new QueryString("?" + string.Join("&", fields.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        var provider = CreateProvider(httpContext);

        // Act
        var result = await provider.ProcessResponseAsync(httpContext);

        // Assert - the complete return marks the payment captured so the booking gets marked paid.
        Assert.True(result.Success);
        Assert.Equal("txn-123", result.TransactionId);
        Assert.Equal("COMPLETE", result.Status);
        Assert.Equal("payment.captured", result.EventType);
    }

    [Fact]
    public async Task ProcessResponseAsync_GetReturnWithPendingStatus_IsNotTreatedAsCaptured()
    {
        // Arrange
        var fields = new Dictionary<string, string>
        {
            ["m_payment_id"] = "txn-123",
            ["pf_payment_id"] = "pf-456",
            ["payment_status"] = "PENDING",
            ["amount_gross"] = "100.00",
        };
        fields["signature"] = ComputeSignature(fields, Options.Passphrase);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.QueryString = new QueryString("?" + string.Join("&", fields.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        var provider = CreateProvider(httpContext);

        // Act
        var result = await provider.ProcessResponseAsync(httpContext);

        // Assert - a pending return keeps the booking pending (the capture ITN flips it later).
        Assert.False(result.Success);
        Assert.Equal("txn-123", result.TransactionId);
        Assert.Equal("PENDING", result.Status);
    }

    [Fact]
    public async Task ProcessResponseAsync_GetReturnWithInvalidSignature_Fails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.QueryString = new QueryString("?m_payment_id=txn-123&payment_status=COMPLETE&signature=not-a-valid-signature");
        var provider = CreateProvider(httpContext);

        // Act
        var result = await provider.ProcessResponseAsync(httpContext);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("webhook.error", result.EventType);
    }

    private static string ComputeSignature(Dictionary<string, string> fields, string passphrase)
    {
        var paramString = string.Join("&", fields.Where(kvp => kvp.Key != "signature").Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var signatureString = string.IsNullOrEmpty(passphrase) ? paramString : $"{paramString}&passphrase={passphrase}";
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(signatureString));
        return Convert.ToHexStringLower(hashBytes);
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
