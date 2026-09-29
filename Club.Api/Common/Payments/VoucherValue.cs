using Club.Common.Enums;
using Club.Entities;

namespace Club.Common.Payments;

public static class VoucherValue
{
    public static decimal Calculate(Voucher voucher, decimal eligibleAmount, decimal availableAmount, decimal remaining, decimal entitlementValue = 0)
    {
        if (eligibleAmount <= 0 || availableAmount <= 0 || remaining <= 0)
            return 0;
        var value = voucher.RedemptionKind switch
        {
            VoucherRedemptionKind.Entitlement => entitlementValue,
            VoucherRedemptionKind.Credit => remaining,
            VoucherRedemptionKind.Discount when voucher.DiscountMode == VoucherDiscountMode.FixedAmount => voucher.DiscountValue ?? 0,
            VoucherRedemptionKind.Discount when voucher.DiscountMode == VoucherDiscountMode.Percentage && voucher.DiscountValue is > 0 and <= 100 =>
                eligibleAmount * voucher.DiscountValue.Value / 100,
            _ => 0,
        };
        if (voucher.RedemptionKind == VoucherRedemptionKind.Discount && voucher.MaxDiscountAmount.HasValue)
            value = Math.Min(value, voucher.MaxDiscountAmount.Value);
        return Math.Max(0, decimal.Round(Math.Min(value, Math.Min(eligibleAmount, availableAmount)), 2, MidpointRounding.AwayFromZero));
    }
}
