namespace Club.Entities;

public class PaymentVoucher
{
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = default!;
    public Guid WalletVoucherGrantId { get; set; }
    public WalletVoucherGrant WalletVoucherGrant { get; set; } = default!;
    public decimal Units { get; set; }
    public int? SlotContractBookingId { get; set; }
    public int? ExtraId { get; set; }
}
