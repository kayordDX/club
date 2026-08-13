namespace Club.Entities;

public class ContractVoucher
{
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public int VoucherId { get; set; }
    public required Voucher Voucher { get; set; }
    public decimal Amount { get; set; } = 1;
}
