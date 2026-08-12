namespace Club.Entities;

public class WalletVoucherGrant
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public required Wallet Wallet { get; set; }

    public int UserContractId { get; set; }
    public required UserContract UserContract { get; set; }

    public int VoucherId { get; set; }
    public required Voucher Voucher { get; set; }

    public decimal AmountGranted { get; set; }
    public decimal AmountRemaining { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
}
