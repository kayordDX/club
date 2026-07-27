using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.Services;

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
        var payment = await dbContext.Payment
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, ct);

        if (payment is null)
        {
            httpContext.Response.StatusCode = 404;
            return;
        }

        if (string.IsNullOrEmpty(payment.FormActionUrl) || string.IsNullOrEmpty(payment.FormFieldsJson))
        {
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync(
                $"Provider '{payment.ProviderName}' does not support form-based payments.", ct);
            return;
        }

        Dictionary<string, string>? fields;
        try
        {
            fields = JsonSerializer.Deserialize<Dictionary<string, string>>(payment.FormFieldsJson);
        }
        catch
        {
            fields = null;
        }

        if (fields is null)
        {
            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsync("Invalid stored form data.", ct);
            return;
        }

        var html = BuildFormHtml(payment.FormActionUrl, fields);

        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html, Encoding.UTF8, ct);

        var logger = httpContext.RequestServices.GetRequiredService<PaymentLogger>();
        await logger.LogAsync(payment.Id, transactionId, payment.ProviderName,
            "payment.form_served", "pending",
            $"Form served for {payment.ProviderName}, action: {payment.FormActionUrl}",
            new { formActionUrl = payment.FormActionUrl }, ct);
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
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
