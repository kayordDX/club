namespace Club.DTO;

public class BookingPaymentDTO
{
    public int BookingId { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountOutstanding { get; set; }
    public decimal AmountAvailable { get; set; }
    public bool IsPaid { get; set; }
    public List<PaymentDTO> Payments { get; set; } = [];
}

public class PaymentDTO
{
    public int Id { get; set; }
    public required string TransactionId { get; set; }
    public decimal Amount { get; set; }
    public int PaymentTypeId { get; set; }
    public required string PaymentType { get; set; }
    public int PaymentStatusId { get; set; }
    public required string PaymentStatus { get; set; }
    public required string ProviderName { get; set; }
    public DateTime PaymentStatusDate { get; set; }
}
