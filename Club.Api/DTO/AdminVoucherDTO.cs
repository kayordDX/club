using Club.Common.Enums;

namespace Club.DTO;

public class AdminVoucherDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsExtra { get; set; }
    public VoucherRedemptionKind RedemptionKind { get; set; }
    public VoucherDiscountMode? DiscountMode { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public bool CanEdit { get; set; }
    public List<int> ContractIds { get; set; } = [];
    public List<int> ExtraIds { get; set; } = [];
}
