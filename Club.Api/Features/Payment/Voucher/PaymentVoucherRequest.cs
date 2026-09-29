namespace Club.Features.Payment.Voucher;

public class PaymentVoucherRequest
{
    public int BookingId { get; set; }
    public Guid GrantId { get; set; }
    public int? SlotContractBookingId { get; set; }
    public int? ExtraId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class PaymentVoucherResponse
{
    public required string TransactionId { get; set; }
    public decimal Amount { get; set; }
    public int PaymentStatusId { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountOutstanding { get; set; }
    public bool IsPaid { get; set; }
}
