using Microsoft.Extensions.Caching.Hybrid;
using Club.Common.Payments;
using Club.Services;

namespace Club.Features.Payment.Result;

internal static class PaymentResultHandler
{
    public static async Task HandleAsync(
        HttpContext httpContext,
        IPaymentFactory paymentFactory,
        HybridCache cache,
        ILogger logger,
        CancellationToken ct)
    {
        var providerName = httpContext.Request.RouteValues["provider"]?.ToString();

        if (string.IsNullOrWhiteSpace(providerName))
        {
            logger.LogWarning("Webhook received with empty provider name.");
            await SendBadRequestAsync(httpContext, ct);
            return;
        }

        IPaymentProvider provider;
        try
        {
            provider = paymentFactory.GetProvider(providerName);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Webhook received for unknown provider '{Provider}': {Message}", providerName, ex.Message);
            await SendBadRequestAsync(httpContext, ct);
            return;
        }

        httpContext.Request.EnableBuffering();

        string transactionId;
        try
        {
            transactionId = await ExtractTransactionIdAsync(httpContext, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract transaction ID from {Provider} webhook payload.", providerName);
            await SendBadRequestAsync(httpContext, ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            logger.LogWarning("No transaction ID could be extracted from {Provider} webhook payload.", providerName);
            await SendBadRequestAsync(httpContext, ct);
            return;
        }

        var cacheKey = $"webhook:{transactionId}";
        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromHours(24)
        };

        try
        {
            var result = await cache.GetOrCreateAsync<PaymentResult>(
                cacheKey,
                async cancellationToken =>
                {
                    logger.LogInformation(
                        "Processing webhook for provider '{Provider}', transaction '{TransactionId}'.",
                        providerName, transactionId);

                    httpContext.Request.Body.Position = 0;

                    return await provider.ProcessResponseAsync(httpContext);
                },
                cacheOptions,
                cancellationToken: ct);

            if (result is null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            logger.LogInformation(
                "Webhook processed for provider '{Provider}', transaction '{TransactionId}': Success={Success}, Event={Event}",
                providerName, result.TransactionId, result.Success, result.EventType);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                result.Success,
                result.TransactionId,
                result.EventType,
                result.Status
            }, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Webhook processing failed for provider '{Provider}', transaction '{TransactionId}'.",
                providerName, transactionId);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Success = false,
                TransactionId = transactionId,
                Error = "Webhook processing failed. Gateway may retry."
            }, cancellationToken: ct);
        }
    }

    private static async Task<string> ExtractTransactionIdAsync(HttpContext context, CancellationToken ct)
    {
        var queryTransactionId = context.Request.Query["merchantTransactionId"].ToString();
        if (!string.IsNullOrWhiteSpace(queryTransactionId))
        {
            return queryTransactionId;
        }

        var payfastQueryId = context.Request.Query["m_payment_id"].ToString();
        if (!string.IsNullOrWhiteSpace(payfastQueryId))
        {
            return payfastQueryId;
        }

        var peachQueryId = context.Request.Query["id"].ToString();
        if (!string.IsNullOrWhiteSpace(peachQueryId))
        {
            return peachQueryId;
        }

        var resourcePath = context.Request.Query["resourcePath"].ToString();
        if (!string.IsNullOrWhiteSpace(resourcePath))
        {
            return resourcePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        }

        context.Request.Body.Position = 0;

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);

        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("merchantTransactionId", out var mti) &&
                mti.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return mti.GetString() ?? string.Empty;
            }

            if (doc.RootElement.TryGetProperty("m_payment_id", out var mpi) &&
                mpi.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return mpi.GetString() ?? string.Empty;
            }

            if (doc.RootElement.TryGetProperty("id", out var id) &&
                id.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return id.GetString() ?? string.Empty;
            }
        }
        catch
        {
        }

        var parsed = System.Web.HttpUtility.ParseQueryString(body);
        var extractedId = parsed["m_payment_id"] ?? parsed["merchantTransactionId"] ?? parsed["id"];

        return extractedId ?? string.Empty;
    }

    private static async Task SendBadRequestAsync(HttpContext context, CancellationToken ct)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            Success = false,
            Error = "Invalid webhook request."
        }, cancellationToken: ct);
    }
}
