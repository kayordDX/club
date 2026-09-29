using Club.Common.Enums;

namespace Club.Features.Admin.Voucher.Create;

public class AdminVoucherCreateRequest
{
    public int FacilityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsExtra { get; set; }
    public VoucherRedemptionKind RedemptionKind { get; set; }
    public VoucherDiscountMode? DiscountMode { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public List<int> ContractIds { get; set; } = [];
    public List<int> ExtraIds { get; set; } = [];
}
