namespace Club.Entities;

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public required Wallet Wallet { get; set; }
    public decimal Amount { get; set; }
    public int WalletTransactionStatusId { get; set; }
    public required WalletTransactionStatus WalletTransactionStatus { get; set; }
    public int WalletTransactionTypeId { get; set; }
    public required WalletTransactionType WalletTransactionType { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string ReferenceId { get; set; }
}
