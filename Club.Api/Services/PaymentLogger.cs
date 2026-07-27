using System.Text.Json;
using Club.Data;
using Club.Entities;

namespace Club.Services;

public class PaymentLogger(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task LogAsync(
        int paymentId,
        string transactionId,
        string providerName,
        string eventType,
        string status,
        string? message = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var log = new PaymentLog
        {
            PaymentId = paymentId,
            TransactionId = transactionId,
            ProviderName = providerName,
            EventType = eventType,
            Status = status,
            Message = message,
            Metadata = metadata is not null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PaymentLog.Add(log);
        await _dbContext.SaveChangesAsync(ct);
    }
}
