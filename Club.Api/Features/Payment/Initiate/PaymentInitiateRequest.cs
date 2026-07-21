namespace Club.Features.Payment.Initiate;

public class PaymentInitiateRequest
{
    public int BookingId { get; set; }
    public required string ProviderName { get; set; }
}
