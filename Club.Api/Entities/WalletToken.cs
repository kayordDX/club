namespace Club.Entities;

public class WalletToken
{
    public Guid WalletId { get; set; }
    public required Wallet Wallet { get; set; }
    public int TokenTypeId { get; set; }
    public required TokenType TokenType { get; set; }
    public decimal Amount { get; set; }
    public DateTime EndDate { get; set; }
}
