using FastEndpoints;

namespace Club.Features.Payment.Events;

public class PaymentSucceededEvent : IEvent
{
    public required string TransactionId { get; set; }
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string? ProviderReference { get; set; }
}
