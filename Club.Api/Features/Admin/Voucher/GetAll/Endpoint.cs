using Club.Common;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Voucher.GetAll;

public class Endpoint(AppDbContext db) : Endpoint<AdminVoucherGetAllRequest, List<AdminVoucherDTO>>
{
    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/voucher");
        Description(x => x.WithName("AdminVoucherGetAll"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminVoucherGetAllRequest req, CancellationToken ct)
    {
        var vouchers = await db
            .Voucher.Where(x => db.VoucherFacility.Any(f => f.VoucherId == x.Id && f.FacilityId == req.FacilityId))
            .Where(x => !db.VoucherFacility.Any(f => f.VoucherId == x.Id && f.FacilityId != req.FacilityId))
            .OrderBy(x => x.Name)
            .Select(x => new AdminVoucherDTO
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsExtra = x.IsExtra,
                RedemptionKind = x.RedemptionKind,
                DiscountMode = x.DiscountMode,
                DiscountValue = x.DiscountValue,
                MaxDiscountAmount = x.MaxDiscountAmount,
            })
            .ToListAsync(ct);
        await Send.OkAsync(vouchers, ct);
    }
}
