using Club.Common.Enums;
using FluentValidation;

namespace Club.Features.Admin.Voucher.Create;

public class AdminVoucherCreateValidator : Validator<AdminVoucherCreateRequest>
{
    public AdminVoucherCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.RedemptionKind).IsInEnum();
        RuleFor(x => x.DiscountMode).IsInEnum().When(x => x.DiscountMode.HasValue);

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (request.RedemptionKind == VoucherRedemptionKind.Discount)
                    {
                        if (!request.DiscountMode.HasValue)
                            context.AddFailure(nameof(request.DiscountMode), "Discount mode is required for discount vouchers.");
                        if (!request.DiscountValue.HasValue || request.DiscountValue <= 0 || !HaveAtMostTwoDecimals(request.DiscountValue))
                            context.AddFailure(nameof(request.DiscountValue), "Discount value must be positive and have at most two decimal places.");
                        if (request.DiscountMode == VoucherDiscountMode.Percentage && request.DiscountValue > 100)
                            context.AddFailure(nameof(request.DiscountValue), "Percentage discounts cannot exceed 100.");
                        if (request.MaxDiscountAmount.HasValue && (request.MaxDiscountAmount <= 0 || !HaveAtMostTwoDecimals(request.MaxDiscountAmount)))
                            context.AddFailure(nameof(request.MaxDiscountAmount), "Discount cap must be positive and have at most two decimal places.");
                    }
                    else
                    {
                        if (request.DiscountMode.HasValue)
                            context.AddFailure(nameof(request.DiscountMode), "Discount fields are only valid for discount vouchers.");
                        if (request.DiscountValue.HasValue)
                            context.AddFailure(nameof(request.DiscountValue), "Discount fields are only valid for discount vouchers.");
                        if (request.MaxDiscountAmount.HasValue)
                            context.AddFailure(nameof(request.MaxDiscountAmount), "Discount fields are only valid for discount vouchers.");
                    }
                }
            );
    }

    private static bool HaveAtMostTwoDecimals(decimal? value) => !value.HasValue || decimal.Round(value.Value, 2) == value.Value;
}
