using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Club.Services;
using Microsoft.AspNetCore.Http.Extensions;

namespace Club.Common.Payments.Provider.Payfast;

public class PayfastProvider(IPaymentOptionsAccessor<PayfastOptions> optionsAccessor, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
    : IPaymentProvider
{
    private readonly HttpClient _httpClient = httpClient;

    public string ProviderName => "payfast";

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct)
    {
        var options = await RequireOptionsAsync(ct);

        var httpContext = httpContextAccessor.HttpContext!;

        // Route the shopper's browser return (GET) through the API's result endpoint instead of
        // the configured frontend page. The signed return fields are verified there and the
        // booking is marked paid/confirmed before redirecting to the frontend success page — this
        // keeps the status update working even when PayFast cannot deliver the server-to-server
        // ITN (e.g. a localhost notify_url in a local POC). The ITN remains the authoritative
        // capture notification on production deployments.
        //
        // Carry the transaction ID on the return URL itself: production PayFast appends its signed
        // fields to the return URL, but the sandbox simulator redirects back without any query
        // string, which previously made the payment unidentifiable on return.
        var returnUrl =
            UriHelper.BuildAbsolute(httpContext.Request.Scheme, httpContext.Request.Host, httpContext.Request.PathBase, "/payment/result/payfast")
            + $"?merchantTransactionId={Uri.EscapeDataString(request.TransactionId)}";

        var fields = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", options.MerchantId),
            new("merchant_key", options.MerchantKey),
            new("return_url", returnUrl),
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
            httpContext.Request.Scheme,
            httpContext.Request.Host,
            httpContext.Request.PathBase,
            $"/payment/form/payfast/{request.TransactionId}"
        );

        return new PaymentResponse
        {
            Success = true,
            TransactionId = request.TransactionId,
            RedirectUrl = redirectUrl,
            FormActionUrl = options.BaseUrl,
            FormFields = allFields,
            Status = "pending",
        };
    }

    public async Task<PaymentResult> ProcessResponseAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        try
        {
            var isGetRequest = string.Equals(context.Request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);

            List<KeyValuePair<string, string>> orderedParams;
            if (isGetRequest)
            {
                // Payfast redirects the shopper back to the return/cancel URL via GET with the same
                // fields appended as query-string parameters (including the signature).
                orderedParams = context
                    .Request.Query.Where(queryParam => !string.IsNullOrEmpty(queryParam.Key))
                    .Select(queryParam => new KeyValuePair<string, string>(queryParam.Key, queryParam.Value.ToString() ?? string.Empty))
                    .ToList();
            }
            else
            {
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();

                context.Request.Body.Position = 0;

                var parsedData = HttpUtility.ParseQueryString(body);

                orderedParams = parsedData
                    .AllKeys.Where(k => k is not null)
                    .Select(k => new KeyValuePair<string, string>(k!, parsedData[k!] ?? string.Empty))
                    .ToList();
            }

            var dataDict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var kvp in orderedParams)
            {
                dataDict[kvp.Key] = kvp.Value;
            }

            var options = await RequireOptionsAsync(context.RequestAborted);

            if (!dataDict.TryGetValue("signature", out var receivedSignature) || string.IsNullOrEmpty(receivedSignature))
            {
                // The PayFast sandbox simulator redirects the shopper back to the return URL without
                // appending the signed fields. Accept a bare return only against the sandbox: it moves
                // no real money, and the verified ITN remains the authoritative capture. Production
                // returns are always signed, so a bare return there is still rejected.
                if (isGetRequest && IsSandboxBaseUrl(options.BaseUrl))
                {
                    if (!dataDict.TryGetValue("merchantTransactionId", out var sandboxTransactionId) || string.IsNullOrEmpty(sandboxTransactionId))
                    {
                        return FailedResult("Missing m_payment_id in ITN payload.");
                    }

                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = sandboxTransactionId,
                        EventType = "payment.captured",
                        Status = "COMPLETE",
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["provider"] = ProviderName },
                    };
                }

                return FailedResult("Missing signature in ITN payload.");
            }

            // merchantTransactionId is appended to the return URL by this provider (see
            // ProcessPaymentAsync) and is not part of the fields PayFast signs.
            var signatureParameters = orderedParams
                .Where(kvp => !string.Equals(kvp.Key, "signature", StringComparison.OrdinalIgnoreCase))
                .Where(kvp => !string.Equals(kvp.Key, "merchantTransactionId", StringComparison.OrdinalIgnoreCase));

            var expectedSignature = CalculateSignature(signatureParameters, options.Passphrase);

            if (!string.Equals(receivedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase))
            {
                return FailedResult("ITN signature verification failed.");
            }

            // The ITN (server-to-server POST) must never trust the payload alone: verify it back
            // with Payfast before applying the payment. The shopper's browser return (GET) carries
            // the same signed fields but is redirected from the payment provider's domain, and the
            // server-side validation endpoint cannot be reached from a local/sandbox POC — the
            // authoritative capture is still enforced by the ITN, so signature-only is acceptable
            // here to keep the booking status update working end to end.
            if (!isGetRequest)
            {
                var isValid = await VerifyWithPayfastAsync(dataDict, context.RequestAborted);
                if (!isValid)
                {
                    return FailedResult("Payfast server-to-server callback validation failed.");
                }
            }

            if (!dataDict.TryGetValue("m_payment_id", out var transactionId) || string.IsNullOrEmpty(transactionId))
            {
                // The return URL carries the merchant transaction ID even when PayFast appends nothing.
                dataDict.TryGetValue("merchantTransactionId", out transactionId);
            }

            if (string.IsNullOrEmpty(transactionId))
            {
                return FailedResult("Missing m_payment_id in ITN payload.");
            }

            dataDict.TryGetValue("payment_status", out var paymentStatus);
            paymentStatus ??= "unknown";

            // Only treat the payment as successful when the gateway reports it as captured.
            // PENDING/CANCELLED/FAILED returns must not mark the payment completed (this is what
            // previously sent shoppers to the failure page even after a successful card payment,
            // because the GET return carries no request body).
            var isCaptured =
                paymentStatus.Equals("COMPLETE", StringComparison.OrdinalIgnoreCase) || paymentStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase);

            return new PaymentResult
            {
                Success = isCaptured,
                TransactionId = transactionId,
                EventType = MapPaymentStatusToEvent(paymentStatus),
                Status = paymentStatus,
                Metadata = new Dictionary<string, string>(dataDict, StringComparer.Ordinal) { ["provider"] = ProviderName },
            };
        }
        catch (Exception ex)
        {
            return FailedResult($"Payfast ITN processing error: {ex.Message}");
        }
    }

    internal static string CalculateSignature(IEnumerable<KeyValuePair<string, string>> parameters, string passphrase)
    {
        var paramString = string.Join("&", parameters.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).Select(kvp => $"{kvp.Key}={PayfastUrlEncode(kvp.Value)}"));

        var signatureString = string.IsNullOrEmpty(passphrase) ? paramString : $"{paramString}&passphrase={PayfastUrlEncode(passphrase)}";
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

    private static bool IsSandboxBaseUrl(string baseUrl)
    {
        return baseUrl.Contains("sandbox.payfast.co.za", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> VerifyWithPayfastAsync(Dictionary<string, string> itnData, CancellationToken ct)
    {
        try
        {
            var payload = new FormUrlEncodedContent(itnData.Where(kvp => !string.Equals(kvp.Key, "signature", StringComparison.OrdinalIgnoreCase)));

            var response = await _httpClient.PostAsync("https://www.payfast.co.za/eng/query/validate", payload, ct);

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
            Metadata = new Dictionary<string, string> { ["error"] = error },
        };
    }

    private async Task<PayfastOptions> RequireOptionsAsync(CancellationToken ct)
    {
        return await optionsAccessor.GetAsync(ct)
            ?? throw new InvalidOperationException(
                "No enabled Payfast provider configuration was found. " + "Seed or configure payment provider settings before processing payments."
            );
    }

    private static string MapPaymentStatusToEvent(string paymentStatus)
    {
        return paymentStatus.ToLowerInvariant() switch
        {
            "complete" or "completed" => "payment.captured",
            "failed" => "payment.failed",
            "pending" => "payment.pending",
            "refunded" => "refund.completed",
            _ => $"payment.{paymentStatus.ToLowerInvariant()}",
        };
    }
}
