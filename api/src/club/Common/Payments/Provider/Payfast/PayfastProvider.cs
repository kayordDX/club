using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using Club.Services;

namespace Club.Common.Payments.Provider.Payfast;

public class PayfastProvider(IOptions<PayfastOptions> options, HttpClient httpClient) : IPaymentProvider
{
    private readonly PayfastOptions _options = options.Value;
    private readonly HttpClient _httpClient = httpClient;

    public string ProviderName => "payfast";

    public Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct)
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["merchant_id"] = _options.MerchantId,
            ["merchant_key"] = _options.MerchantKey,
            ["return_url"] = _options.ReturnUrl,
            ["cancel_url"] = _options.CancelUrl,
            ["notify_url"] = _options.NotifyUrl,
            ["m_payment_id"] = request.TransactionId,
            ["amount"] = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
            ["item_name"] = request.Description ?? "Club payment",
        };

        var signature = CalculateSignature(parameters);
        parameters["signature"] = signature;

        var queryString = string.Join("&", parameters.Select(kvp =>
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

        var redirectUrl = $"{_options.BaseUrl.TrimEnd('/')}/process?{queryString}";

        return Task.FromResult(new PaymentResponse
        {
            Success = true,
            TransactionId = request.TransactionId,
            RedirectUrl = redirectUrl,
            Status = "pending"
        });
    }

    public async Task<PaymentResult> ProcessResponseAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        try
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            context.Request.Body.Position = 0;

            var parsedData = HttpUtility.ParseQueryString(body);
            var itnData = parsedData.AllKeys
                .Where(k => k is not null)
                .ToDictionary(k => k!, k => parsedData[k!] ?? string.Empty, StringComparer.Ordinal);

            var receivedSignature = itnData.GetValueOrDefault("signature");
            if (string.IsNullOrEmpty(receivedSignature))
            {
                return FailedResult("Missing signature in ITN payload.");
            }

            var signatureParameters = itnData
                .Where(kvp => !string.Equals(kvp.Key, "signature", StringComparison.OrdinalIgnoreCase))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

            var expectedSignature = CalculateSignature(signatureParameters);

            if (!string.Equals(receivedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase))
            {
                return FailedResult("ITN signature verification failed.");
            }

            var isValid = await VerifyWithPayfastAsync(itnData, context.RequestAborted);
            if (!isValid)
            {
                return FailedResult("Payfast server-to-server callback validation failed.");
            }

            if (!itnData.TryGetValue("m_payment_id", out var transactionId) ||
                string.IsNullOrEmpty(transactionId))
            {
                return FailedResult("Missing m_payment_id in ITN payload.");
            }

            if (!itnData.TryGetValue("payment_status", out var paymentStatus))
            {
                paymentStatus = "unknown";
            }

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                EventType = MapPaymentStatusToEvent(paymentStatus),
                Status = paymentStatus,
                Metadata = new Dictionary<string, string>(itnData, StringComparer.Ordinal)
                {
                    ["provider"] = ProviderName
                }
            };
        }
        catch (Exception ex)
        {
            return FailedResult($"Payfast ITN processing error: {ex.Message}");
        }
    }

    private string CalculateSignature(IDictionary<string, string> parameters)
    {
        var paramString = string.Join("&",
            parameters
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));

        var signatureString = $"{paramString}&passphrase={HttpUtility.UrlEncode(_options.Passphrase)}";
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(signatureString));

        return Convert.ToHexStringLower(hashBytes);
    }

    private async Task<bool> VerifyWithPayfastAsync(Dictionary<string, string> itnData, CancellationToken ct)
    {
        try
        {
            var payload = new FormUrlEncodedContent(itnData
                .Where(kvp => !string.Equals(kvp.Key, "signature", StringComparison.OrdinalIgnoreCase)));

            var response = await _httpClient.PostAsync(
                "https://www.payfast.co.za/eng/query/validate", payload, ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            return body.TrimStart().StartsWith("VALID", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static PaymentResult FailedResult(string error)
    {
        return new PaymentResult
        {
            Success = false,
            TransactionId = "unknown",
            EventType = "webhook.error",
            Status = "failed",
            Metadata = new Dictionary<string, string>
            {
                ["error"] = error
            }
        };
    }

    private static string MapPaymentStatusToEvent(string paymentStatus)
    {
        return paymentStatus.ToLowerInvariant() switch
        {
            "complete" or "completed" => "payment.captured",
            "failed" => "payment.failed",
            "pending" => "payment.pending",
            "refunded" => "refund.completed",
            _ => $"payment.{paymentStatus.ToLowerInvariant()}"
        };
    }
}
