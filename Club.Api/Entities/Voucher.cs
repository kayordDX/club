using Club.Common.Enums;

namespace Club.Entities;

public class Voucher
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsExtra { get; set; }
    public VoucherRedemptionKind RedemptionKind { get; set; } = VoucherRedemptionKind.Entitlement;
    public VoucherDiscountMode? DiscountMode { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
}
