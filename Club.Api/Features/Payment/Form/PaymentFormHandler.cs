using System.Text;
using System.Text.Json;
using Club.Common.Enums;
using Club.Common.Payments;
using Club.Data;
using Club.Services;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Payment.Form;

internal static class PaymentFormHandler
{
    public static async Task HandleAsync(HttpContext httpContext, CancellationToken ct)
    {
        var providerName = httpContext.Request.RouteValues["provider"]?.ToString();
        var transactionId = httpContext.Request.RouteValues["transactionId"]?.ToString();

        if (string.IsNullOrEmpty(transactionId))
        {
            httpContext.Response.StatusCode = 404;
            return;
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<AppDbContext>();
        var payment = await dbContext.Payment.FirstOrDefaultAsync(p => p.TransactionId == transactionId, ct);

        if (payment is null || !string.Equals(payment.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.StatusCode = 404;
            return;
        }

        if (payment.PaymentStatusId != (int)PaymentStatusEnum.Pending)
        {
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync(
                "This payment attempt is no longer pending. Return to the booking to check the balance or start another payment.",
                ct
            );
            return;
        }

        Dictionary<string, string>? fields;
        try
        {
            fields = string.IsNullOrEmpty(payment.FormFieldsJson) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(payment.FormFieldsJson);
        }
        catch
        {
            fields = null;
        }

        if (string.IsNullOrWhiteSpace(payment.FormActionUrl) || fields is not { Count: > 0 })
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
            var bookingId = await dbContext.PaymentBooking.Where(x => x.PaymentId == payment.Id).Select(x => (int?)x.BookingId).FirstOrDefaultAsync(ct);
            if (bookingId.HasValue)
                await BookingPayments.LockAsync(dbContext, bookingId.Value, ct);
            await dbContext.Entry(payment).ReloadAsync(ct);
            if (payment.PaymentStatusId == (int)PaymentStatusEnum.Pending)
            {
                payment.PaymentStatusId = (int)PaymentStatusEnum.Failed;
                payment.PaymentStatusDate = DateTime.UtcNow;
                payment.ErrorMessage = "Payment checkout form is missing or invalid. Return to the booking and try again.";
                await dbContext.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync(
                "Payment checkout form is missing or invalid. Return to the booking and try again; this attempt has not reduced your balance.",
                ct
            );
            return;
        }

        var html = BuildFormHtml(payment.FormActionUrl, fields);

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html, Encoding.UTF8, ct);

        var logger = httpContext.RequestServices.GetRequiredService<PaymentLogger>();
        await logger.LogAsync(
            payment.Id,
            transactionId,
            payment.ProviderName,
            "payment.form_served",
            "pending",
            $"Form served for {payment.ProviderName}, action: {payment.FormActionUrl}",
            new { formActionUrl = payment.FormActionUrl },
            ct
        );
    }

    private static string BuildFormHtml(string actionUrl, Dictionary<string, string> fields)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><body onload=\"document.forms[0].submit()\">");
        sb.Append($"<form action=\"{HtmlEncode(actionUrl)}\" method=\"post\">");

        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field.Value))
                continue;
            sb.Append($"<input type=\"hidden\" name=\"{HtmlEncode(field.Key)}\" value=\"{HtmlEncode(field.Value)}\" />");
        }

        sb.Append("</form></body></html>");

        return sb.ToString();
    }

    private static string HtmlEncode(string value)
    {
        return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&#39;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
