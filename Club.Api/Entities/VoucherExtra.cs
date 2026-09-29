namespace Club.Entities;

public class VoucherExtra
{
    public int VoucherId { get; set; }
    public required Voucher Voucher { get; set; }
    public int ExtraId { get; set; }
    public required Extra Extra { get; set; }
}
