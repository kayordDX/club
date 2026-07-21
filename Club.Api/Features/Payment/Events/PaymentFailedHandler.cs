using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.Entities;

namespace Club.Features.Payment.Events;

public class PaymentFailedHandler(AppDbContext dbContext, ILogger<PaymentFailedHandler> logger)
    : IEventHandler<PaymentFailedEvent>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<PaymentFailedHandler> _logger = logger;

    public async Task HandleAsync(PaymentFailedEvent eventModel, CancellationToken ct)
    {
        var payment = await _dbContext.Payment
            .FirstOrDefaultAsync(p => p.Id == eventModel.PaymentId, ct);

        if (payment is null)
        {
            _logger.LogWarning(
                "PaymentFailedEvent: Payment {PaymentId} not found.",
                eventModel.PaymentId);
            return;
        }

        payment.PaymentStatusId = (int)Common.Enums.PaymentStatusEnum.Failed;
        payment.PaymentStatusDate = DateTime.UtcNow;
        payment.ErrorMessage = eventModel.ErrorMessage;

        await _dbContext.SaveChangesAsync(ct);
    }
}
