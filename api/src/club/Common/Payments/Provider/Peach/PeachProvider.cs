using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Club.Common.Payments;
using Club.Services;

namespace Club.Common.Payments.Provider.Peach;

public class PeachProvider(
    IPaymentOptionsAccessor<PeachOptions> optionsAccessor,
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor) : IPaymentProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string ProviderName => "peach";

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct)
    {
        try
        {
            var options = await RequireOptionsAsync(ct);
            var shopperResultUrl = BuildShopperResultUrl();

            var payload = new
            {
                authentication = new
                {
                    userId = options.UserId,
                    password = options.Password,
                    entityId = options.EntityId
                },
                merchantTransactionId = request.TransactionId,
                amount = request.Amount.ToString("F2"),
                currency = request.Currency,
                paymentBrand = "PEACHEFT",
                paymentType = "DB",
                shopperResultUrl
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/payments")
            {
                Content = JsonContent.Create(payload)
            };

            var response = await _httpClient.SendAsync(httpRequest, ct);
            var result = await response.Content.ReadFromJsonAsync<PeachCheckoutResponse>(cancellationToken: ct);

            if (result is null || !response.IsSuccessStatusCode)
            {
                return new PaymentResponse
                {
                    Success = false,
                    TransactionId = request.TransactionId,
                    ErrorMessage = result?.Result?.Description ?? "Peach Payments checkout request failed."
                };
            }

            return new PaymentResponse
            {
                Success = true,
                TransactionId = request.TransactionId,
                ProviderReference = result.Id,
                RedirectUrl = result.Redirect?.Url,
                Status = result.Result?.Code
            };
        }
        catch (Exception ex)
        {
            return new PaymentResponse
            {
                Success = false,
                TransactionId = request.TransactionId,
                ErrorMessage = $"Peach Payments error: {ex.Message}"
            };
        }
    }

    public async Task<PaymentResult> ProcessResponseAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        try
        {
            var options = await RequireOptionsAsync(context.RequestAborted);

            PeachPaymentPayload? payload;

            if (HasRedirectQuery(context.Request))
            {
                payload = await QueryPaymentStatusAsync(context.Request, options, context.RequestAborted);
            }
            else
            {
                payload = await System.Text.Json.JsonSerializer.DeserializeAsync<PeachPaymentPayload>(
                    context.Request.Body,
                    cancellationToken: context.RequestAborted);

                // Ensure stream can be re-read if needed downstream
                context.Request.Body.Position = 0;
            }

            if (payload is null)
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = "unknown",
                    EventType = "unknown",
                    Status = "failed",
                    Metadata = new Dictionary<string, string>
                    {
                        ["error"] = "Empty or unparseable Peach payment response received."
                    }
                };
            }

            return new PaymentResult
            {
                Success = true,
                TransactionId = payload.MerchantTransactionId ?? payload.Id ?? "unknown",
                EventType = payload.PaymentType,
                Status = payload.Result?.Code,
                Metadata = new Dictionary<string, string>
                {
                    ["provider"] = ProviderName,
                    ["providerReference"] = payload.Id ?? string.Empty,
                    ["resultDescription"] = payload.Result?.Description ?? string.Empty,
                    ["redirectUrl"] = payload.Redirect?.Url ?? string.Empty,
                    ["paymentBrand"] = payload.PaymentBrand ?? string.Empty,
                    ["resourcePath"] = context.Request.Query["resourcePath"].ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                TransactionId = "unknown",
                EventType = "webhook.error",
                Status = "failed",
                Metadata = new Dictionary<string, string>
                {
                    ["error"] = $"Failed to process Peach Payments response: {ex.Message}"
                }
            };
        }
    }

    private static bool HasRedirectQuery(HttpRequest request)
    {
        return request.Query.ContainsKey("resourcePath") || request.Query.ContainsKey("id");
    }

    private async Task<PeachPaymentPayload?> QueryPaymentStatusAsync(
        HttpRequest request,
        PeachOptions options,
        CancellationToken ct)
    {
        var resourcePath = request.Query["resourcePath"].ToString();
        var id = request.Query["id"].ToString();

        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            resourcePath = $"/payments/{id}";
        }

        var uri = QueryHelpers.AddQueryString(
            $"{options.BaseUrl.TrimEnd('/')}{resourcePath}",
            new Dictionary<string, string?>
            {
                ["authentication.entityId"] = options.EntityId,
                ["authentication.userId"] = options.UserId,
                ["authentication.password"] = options.Password
            });

        var response = await _httpClient.GetAsync(uri, ct);
        var payload = await response.Content.ReadFromJsonAsync<PeachPaymentPayload>(cancellationToken: ct);

        if (payload is null || !response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                payload?.Result?.Description ?? "Failed to query Peach payment status from shopper result callback.");
        }

        return payload;
    }

    private string BuildShopperResultUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException(
                "Unable to build Peach shopperResultUrl because there is no active HTTP request.");

        return UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            $"/payment/result/{ProviderName}");
    }

    private async Task<PeachOptions> RequireOptionsAsync(CancellationToken ct)
    {
        return await optionsAccessor.GetAsync(ct)
            ?? throw new InvalidOperationException(
                "No enabled Peach provider configuration was found. " +
                "Seed or configure payment provider settings before processing payments.");
    }

    private sealed class PeachCheckoutResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("result")]
        public PeachResultPayload? Result { get; set; }

        [JsonPropertyName("redirect")]
        public PeachRedirectPayload? Redirect { get; set; }
    }

    private sealed class PeachRefundResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("resultCode")]
        public string? ResultCode { get; set; }

        [JsonPropertyName("resultDescription")]
        public string? ResultDescription { get; set; }
    }

    private sealed class PeachPaymentPayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("merchantTransactionId")]
        public string? MerchantTransactionId { get; set; }

        [JsonPropertyName("paymentType")]
        public string? PaymentType { get; set; }

        [JsonPropertyName("paymentBrand")]
        public string? PaymentBrand { get; set; }

        [JsonPropertyName("result")]
        public PeachResultPayload? Result { get; set; }

        [JsonPropertyName("redirect")]
        public PeachRedirectPayload? Redirect { get; set; }
    }

    private sealed class PeachResultPayload
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private sealed class PeachRedirectPayload
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("method")]
        public string? Method { get; set; }
    }
}
