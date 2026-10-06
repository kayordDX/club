using Club.Features.Admin.Voucher.Create;

namespace Club.Features.Admin.Voucher.Update;

public class AdminVoucherUpdateValidator : Validator<AdminVoucherUpdateRequest>
{
    public AdminVoucherUpdateValidator()
    {
        Include(new AdminVoucherCreateValidator());
    }
}
