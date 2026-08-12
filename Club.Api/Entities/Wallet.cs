namespace Club.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required User User { get; set; }
    public bool IsActive { get; set; } = true;
    public string Currency { get; set; } = "ZAR";

    public WalletBalance? Balance { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = [];
    public ICollection<WalletVoucherGrant> VoucherGrants { get; set; } = [];
}
