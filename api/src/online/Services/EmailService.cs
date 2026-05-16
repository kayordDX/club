using System.Text.Json;
using Online.Data;
using Online.Entities;
using Online.Models;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Online.Services;

public interface IEmailService
{
    Task EnqueueEmailAsync(
        List<EmailTarget> to,
        string subject,
        string message,
        List<EmailTarget>? cc = null,
        List<EmailTarget>? bcc = null,
        bool isHtml = true,
        string? replyTo = null,
        CancellationToken cancellationToken = default
    );
}

public class EmailService(AppDbContext dbContext, ITimeTickerManager<TimeTickerEntity> tickerManager) : IEmailService
{
    public async Task EnqueueEmailAsync(
        List<EmailTarget> to,
        string subject,
        string message,
        List<EmailTarget>? cc = null,
        List<EmailTarget>? bcc = null,
        bool isHtml = true,
        string? replyTo = null,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new EmailPayload(to, cc, bcc, isHtml, replyTo);

        await dbContext.EmailLog.AddAsync(new EmailLog
        {
            Payload = JsonSerializer.Serialize(payload),
            Subject = subject,
            Message = message,
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await tickerManager.AddAsync(new TimeTickerEntity
        {
            Function = "EmailJob",
            ExecutionTime = DateTime.UtcNow,
        }, cancellationToken);
    }
}
