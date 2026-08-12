namespace Club.Entities;

public class WalletBalance
{
    public Guid WalletId { get; set; }
    public required Wallet Wallet { get; set; }
    public decimal Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
}
