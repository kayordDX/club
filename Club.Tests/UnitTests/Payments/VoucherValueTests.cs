using Club.Common.Enums;
using Club.Common.Payments;
using Club.Entities;

namespace UnitTests.Payments;

public class VoucherValueTests
{
    [Theory]
    [InlineData(VoucherRedemptionKind.Entitlement, null, null, null, 5, 200, 200)]
    [InlineData(VoucherRedemptionKind.Credit, null, null, null, 75, 0, 75)]
    [InlineData(VoucherRedemptionKind.Discount, VoucherDiscountMode.Percentage, 25, null, 1, 0, 100)]
    [InlineData(VoucherRedemptionKind.Discount, VoucherDiscountMode.Percentage, 25, 50, 1, 0, 50)]
    [InlineData(VoucherRedemptionKind.Discount, VoucherDiscountMode.FixedAmount, 150, null, 1, 0, 150)]
    [InlineData(VoucherRedemptionKind.Discount, VoucherDiscountMode.Percentage, 110, null, 1, 0, 0)]
    public void CalculatesValue(VoucherRedemptionKind kind, VoucherDiscountMode? mode, int? value, int? cap, int remaining, int entitlementValue, int expected)
    {
        var voucher = new Voucher
        {
            Name = "Test",
            RedemptionKind = kind,
            DiscountMode = mode,
            DiscountValue = value,
            MaxDiscountAmount = cap,
        };
        Assert.Equal(expected, VoucherValue.Calculate(voucher, 400, 400, remaining, entitlementValue));
    }

    [Fact]
    public void CapsAtEligibleSubtotalAndOutstanding()
    {
        var voucher = new Voucher
        {
            Name = "Fixed",
            RedemptionKind = VoucherRedemptionKind.Discount,
            DiscountMode = VoucherDiscountMode.FixedAmount,
            DiscountValue = 500,
        };
        Assert.Equal(40, VoucherValue.Calculate(voucher, 100, 40, 1));
        Assert.Equal(25, VoucherValue.Calculate(voucher, 25, 40, 1));
        Assert.Equal(0, VoucherValue.Calculate(voucher, 25, 40, 0));
    }

    [Theory]
    [InlineData(0, 100, false)]
    [InlineData(-1, 100, false)]
    [InlineData(101, 100, false)]
    [InlineData(0.001, 100, false)]
    [InlineData(40, 100, true)]
    [InlineData(100, 100, true)]
    public void ValidatesSplitAmount(double amount, double available, bool expected) =>
        Assert.Equal(expected, BookingPayments.IsValidAmount((decimal)amount, (decimal)available));
}
