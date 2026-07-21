using Microsoft.EntityFrameworkCore;
using Club.Common.Payments;
using Club.Data;
using Club.Entities;
using Club.Features.Payment.Events;
using FastEndpoints;
using Club.Services;

namespace Club.Features.Payment.Result;

internal static class PaymentResultHandler
{
    public static async Task HandleAsync(
        HttpContext httpContext,
        IPaymentFactory paymentFactory,
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

        PaymentResult? result = null;
        try
        {
            logger.LogInformation(
                "Processing webhook for provider '{Provider}', transaction '{TransactionId}'.",
                providerName, transactionId);

            httpContext.Request.Body.Position = 0;

            result = await provider.ProcessResponseAsync(httpContext);

            logger.LogInformation(
                "Webhook processed for provider '{Provider}', transaction '{TransactionId}': Success={Success}, Event={Event}",
                providerName, result.TransactionId, result.Success, result.EventType);

            await PersistAndUpdateBookingAsync(httpContext, result, logger, ct);

            var isGetRequest = string.Equals(httpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            if (isGetRequest)
            {
                var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                var frontendBaseUrl = configuration["Payment:FrontendBaseUrl"] ?? "http://localhost:5173";

                var queryParams = new Dictionary<string, string>
                {
                    ["transactionId"] = result.TransactionId
                };

                if (!result.Success)
                {
                    queryParams["error"] = result.Metadata?.GetValueOrDefault("error") ?? "Payment processing failed.";
                }

                var isSuccess = result.Success;
                var segment = isSuccess ? "success" : "failure";
                var query = string.Join("&", queryParams.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var redirectUrl = $"{frontendBaseUrl.TrimEnd('/')}/payment/{segment}?{query}";

                httpContext.Response.StatusCode = StatusCodes.Status302Found;
                httpContext.Response.Headers.Location = redirectUrl;
                httpContext.Response.ContentLength = 0;
                await httpContext.Response.StartAsync(ct);
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    result.Success,
                    result.TransactionId,
                    result.EventType,
                    result.Status
                }, cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Webhook processing failed for provider '{Provider}', transaction '{TransactionId}'.",
                providerName, result?.TransactionId ?? transactionId);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Success = false,
                TransactionId = transactionId,
                Error = "Webhook processing failed. Gateway may retry."
            }, cancellationToken: ct);
        }
    }

    private static async Task PersistAndUpdateBookingAsync(
        HttpContext httpContext,
        PaymentResult result,
        ILogger logger,
        CancellationToken ct)
    {
        var dbContext = httpContext.RequestServices.GetRequiredService<AppDbContext>();

        var internalTransactionId = result.TransactionId;
        if (string.IsNullOrWhiteSpace(internalTransactionId) || internalTransactionId == "unknown")
        {
            logger.LogWarning("Payment result has no valid transaction ID.");
            return;
        }

        var payment = await dbContext.Payment
            .FirstOrDefaultAsync(p => p.TransactionId == internalTransactionId, ct);

        if (payment is null)
        {
            logger.LogWarning("Payment record not found for transaction '{TransactionId}'.", internalTransactionId);
            return;
        }

        var paymentBooking = await dbContext.PaymentBooking
            .Include(pb => pb.Booking)
            .FirstOrDefaultAsync(pb => pb.PaymentId == payment.Id, ct);

        var bookingId = paymentBooking?.BookingId ?? 0;

        if (result.Success)
        {
            payment.PaymentStatusId = (int)Common.Enums.PaymentStatusEnum.Completed;
            payment.PaymentStatusDate = DateTime.UtcNow;
            payment.ProviderReference ??= result.Metadata?.GetValueOrDefault("providerReference");

            if (paymentBooking?.Booking is not null)
            {
                var booking = paymentBooking.Booking;
                booking.AmountPaid += payment.Amount;
                booking.AmountOutstanding -= payment.Amount;

                if (booking.AmountOutstanding <= 0)
                {
                    booking.IsPaid = true;
                    booking.BookingStatusId = (int)Common.Enums.BookingStatusEnum.Confirmed;
                    booking.BookingStatusDate = DateTime.UtcNow;
                }
            }

            await dbContext.SaveChangesAsync(ct);

            await new PaymentSucceededEvent
            {
                TransactionId = internalTransactionId,
                PaymentId = payment.Id,
                BookingId = bookingId,
                Amount = payment.Amount,
                ProviderReference = payment.ProviderReference
            }.PublishAsync(Mode.WaitForNone, ct);
        }
        else
        {
            payment.PaymentStatusId = (int)Common.Enums.PaymentStatusEnum.Failed;
            payment.PaymentStatusDate = DateTime.UtcNow;
            payment.ErrorMessage = result.Metadata?.GetValueOrDefault("error");

            await dbContext.SaveChangesAsync(ct);

            await new PaymentFailedEvent
            {
                TransactionId = internalTransactionId,
                PaymentId = payment.Id,
                BookingId = bookingId,
                ErrorMessage = payment.ErrorMessage
            }.PublishAsync(Mode.WaitForNone, ct);
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
