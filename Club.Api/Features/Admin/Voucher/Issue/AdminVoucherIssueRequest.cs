namespace Club.Features.Admin.Voucher.Issue;

public class AdminVoucherIssueRequest
{
    public int FacilityId { get; set; }
    public Guid WalletId { get; set; }
    public int VoucherId { get; set; }
    public int? SourceUserContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }
}
