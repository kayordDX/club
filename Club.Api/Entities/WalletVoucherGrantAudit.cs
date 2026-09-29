using Club.Common.Enums;

namespace Club.Entities;

public class WalletVoucherGrantAudit
{
    public Guid Id { get; set; }
    public Guid WalletVoucherGrantId { get; set; }
    public required WalletVoucherGrant WalletVoucherGrant { get; set; }
    public WalletVoucherGrantAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public WalletVoucherGrantSource SourceType { get; set; }

    // Immutable identifiers, deliberately not FKs: source deletion must not rewrite history.
    public Guid? AssigningUserId { get; set; }
    public int? SourceUserContractId { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }
}
