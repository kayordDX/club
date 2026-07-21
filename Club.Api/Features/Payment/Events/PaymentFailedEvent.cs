using FastEndpoints;

namespace Club.Features.Payment.Events;

public class PaymentFailedEvent : IEvent
{
    public required string TransactionId { get; set; }
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public string? ErrorMessage { get; set; }
}
