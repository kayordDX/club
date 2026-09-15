using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Club.Common.Payments;
using Club.Common.Payments.Provider.Payfast;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Payments;

// Shared fixtures and helpers for the Payfast provider test suites so the sandbox/production options
// and the signature mirror stay in one place and cannot drift between test files.
internal static class PayfastTestHelpers
{
    public static PayfastOptions SandboxOptions() =>
        new()
        {
            MerchantId = "10000100",
            MerchantKey = "46f0cd694581a",
            Passphrase = "jt7NOE43FZPn",
            BaseUrl = "https://sandbox.payfast.co.za/eng/process",
            ReturnUrl = "http://localhost:5173/payment/success",
            CancelUrl = "http://localhost:5173/payment/cancelled",
            NotifyUrl = "http://localhost:5000/payment/result/payfast",
        };

    public static PayfastOptions ProductionOptions() =>
        new()
        {
            MerchantId = "10000100",
            MerchantKey = "46f0cd694581a",
            Passphrase = "jt7NOE43FZPn",
            BaseUrl = "https://www.payfast.co.za/eng/process",
            ReturnUrl = "http://localhost:5173/payment/success",
            CancelUrl = "http://localhost:5173/payment/cancelled",
            NotifyUrl = "http://localhost:5000/payment/result/payfast",
        };

    public static PayfastProvider CreateProvider(HttpContext httpContext, PayfastOptions? options = null)
    {
        return new PayfastProvider(new FakeOptionsAccessor(options ?? SandboxOptions()), new FakeHttpContextAccessor(httpContext), new HttpClient());
    }

    // Independent re-implementation of PayfastProvider.CalculateSignature so a test verifies the real
    // signature against a separate copy of the algorithm rather than the code under test.
    public static string ComputeSignature(IEnumerable<KeyValuePair<string, string>> parameters, string passphrase)
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
