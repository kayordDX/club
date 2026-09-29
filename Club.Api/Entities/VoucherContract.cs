namespace Club.Entities;

public class VoucherContract
{
    public int VoucherId { get; set; }
    public required Voucher Voucher { get; set; }
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
}
