namespace Club.Features.Wallet.Transfer;

public class WalletTransferRequest
{
    public Guid GrantId { get; set; }
    public required string Recipient { get; set; }
}
