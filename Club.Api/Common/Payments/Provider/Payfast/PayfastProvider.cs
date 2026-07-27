using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Club.Services;
using Microsoft.AspNetCore.Http.Extensions;

namespace Club.Common.Payments.Provider.Payfast;

public class PayfastProvider(
    IPaymentOptionsAccessor<PayfastOptions> optionsAccessor,
    IHttpContextAccessor httpContextAccessor,
    HttpClient httpClient) : IPaymentProvider
{
    private readonly HttpClient _httpClient = httpClient;

    public string ProviderName => "payfast";

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct)
    {
        var options = await RequireOptionsAsync(ct);

        var fields = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", options.MerchantId),
            new("merchant_key", options.MerchantKey),
            new("return_url", options.ReturnUrl),
            new("cancel_url", options.CancelUrl),
            new("notify_url", options.NotifyUrl),
            new("m_payment_id", request.TransactionId),
            new("amount", request.Amount.ToString("F2", CultureInfo.InvariantCulture)),
            new("item_name", request.Description ?? "Club payment"),
        };

        var signature = CalculateSignature(fields, options.Passphrase);

        var allFields = new Dictionary<string, string>(fields.Count + 1);
        foreach (var kvp in fields)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
                allFields[kvp.Key] = kvp.Value;
        }
        allFields["signature"] = signature;

        var redirectUrl = UriHelper.BuildAbsolute(
            httpContextAccessor.HttpContext!.Request.Scheme,
            httpContextAccessor.HttpContext.Request.Host,
            httpContextAccessor.HttpContext.Request.PathBase,
            $"/payment/form/payfast/{request.TransactionId}");

        return new PaymentResponse
        {
            Success = true,
            TransactionId = request.TransactionId,
            RedirectUrl = redirectUrl,
            FormActionUrl = options.BaseUrl,
            FormFields = allFields,
            Status = "pending"
        };
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

            var orderedParams = parsedData.AllKeys
                .Where(k => k is not null)
                .Select(k => new KeyValuePair<string, string>(k!, parsedData[k!] ?? string.Empty))
                .ToList();

            var dataDict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kvp in orderedParams)
            {
                dataDict[kvp.Key] = kvp.Value;
            }

            if (!dataDict.TryGetValue("signature", out var receivedSignature) ||
                string.IsNullOrEmpty(receivedSignature))
            {
                return FailedResult("Missing signature in ITN payload.");
            }

            var signatureParameters = orderedParams
                .Where(kvp => !string.Equals(kvp.Key, "signature", StringComparison.OrdinalIgnoreCase));

            var options = await RequireOptionsAsync(context.RequestAborted);
            var expectedSignature = CalculateSignature(signatureParameters, options.Passphrase);

            if (!string.Equals(receivedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase))
            {
                return FailedResult("ITN signature verification failed.");
            }

            var isValid = await VerifyWithPayfastAsync(dataDict, context.RequestAborted);
            if (!isValid)
            {
                return FailedResult("Payfast server-to-server callback validation failed.");
            }

            if (!dataDict.TryGetValue("m_payment_id", out var transactionId) ||
                string.IsNullOrEmpty(transactionId))
            {
                return FailedResult("Missing m_payment_id in ITN payload.");
            }

            dataDict.TryGetValue("payment_status", out var paymentStatus);
            paymentStatus ??= "unknown";

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                EventType = MapPaymentStatusToEvent(paymentStatus),
                Status = paymentStatus,
                Metadata = new Dictionary<string, string>(dataDict, StringComparer.Ordinal)
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

    internal static string CalculateSignature(IEnumerable<KeyValuePair<string, string>> parameters, string passphrase)
    {
        var paramString = string.Join("&",
            parameters
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{kvp.Key}={PayfastUrlEncode(kvp.Value)}"));

        var signatureString = string.IsNullOrEmpty(passphrase)
            ? paramString
            : $"{paramString}&passphrase={PayfastUrlEncode(passphrase)}";
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(signatureString));

        return Convert.ToHexStringLower(hashBytes);
    }

    internal static string PayfastUrlEncode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        var encoded = HttpUtility.UrlEncode(value);
        return Regex.Replace(encoded, @"%[0-9a-f]{2}", m => m.Value.ToUpperInvariant());
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

    private async Task<PayfastOptions> RequireOptionsAsync(CancellationToken ct)
    {
        return await optionsAccessor.GetAsync(ct)
            ?? throw new InvalidOperationException(
                "No enabled Payfast provider configuration was found. " +
                "Seed or configure payment provider settings before processing payments.");
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
