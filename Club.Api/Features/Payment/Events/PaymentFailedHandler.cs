namespace Club.Features.Payment.Events;

public class PaymentFailedHandler(ILogger<PaymentFailedHandler> logger) : IEventHandler<PaymentFailedEvent>
{
    public Task HandleAsync(PaymentFailedEvent eventModel, CancellationToken ct)
    {
        // The callback persists state synchronously; asynchronous handlers must not overwrite a later successful result.
        logger.LogInformation("Payment {PaymentId} failed: {ErrorMessage}", eventModel.PaymentId, eventModel.ErrorMessage);
        return Task.CompletedTask;
    }
}
