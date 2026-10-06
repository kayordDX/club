using Club.Common;
using Club.Data;
using Club.DTO;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Voucher.Create;

public class Endpoint(AppDbContext db) : Endpoint<AdminVoucherCreateRequest, AdminVoucherDTO>
{
    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/voucher");
        Description(x => x.WithName("AdminVoucherCreate"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminVoucherCreateRequest req, CancellationToken ct)
    {
        var facility = await db.Facility.FirstOrDefaultAsync(x => x.Id == req.FacilityId, ct);
        if (facility is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var voucher = new Club.Entities.Voucher
        {
            Name = req.Name.Trim(),
            Description = req.Description ?? string.Empty,
            IsExtra = req.IsExtra,
            RedemptionKind = req.RedemptionKind,
            DiscountMode = req.DiscountMode,
            DiscountValue = req.DiscountValue,
            MaxDiscountAmount = req.MaxDiscountAmount,
        };
        db.Voucher.Add(voucher);
        db.VoucherFacility.Add(new VoucherFacility { Voucher = voucher, Facility = facility });
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(
            new AdminVoucherDTO
            {
                Id = voucher.Id,
                Name = voucher.Name,
                Description = voucher.Description,
                IsExtra = voucher.IsExtra,
                RedemptionKind = voucher.RedemptionKind,
                DiscountMode = voucher.DiscountMode,
                DiscountValue = voucher.DiscountValue,
                MaxDiscountAmount = voucher.MaxDiscountAmount,
            },
            ct
        );
    }
}
