using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.GetVouchers;

public class Endpoint(AppDbContext db) : Endpoint<AdminContractGetVouchersRequest, AdminContractVouchersDTO>
{
    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/contract/{Id}/vouchers");
        Description(x => x.WithName("AdminContractGetVouchers"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractGetVouchersRequest req, CancellationToken ct)
    {
        if (!await db.Contract.AnyAsync(c => c.Id == req.Id && c.ContractFacilities.Any(f => f.FacilityId == req.FacilityId), ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var links = await db
            .ContractVoucher.Where(x => x.ContractId == req.Id)
            .OrderBy(x => x.Voucher.Name)
            .Select(x => new AdminContractVoucherDTO { VoucherId = x.VoucherId, Amount = x.Amount })
            .ToListAsync(ct);
        var vouchers = await db
            .Voucher.Where(x =>
                !db.VoucherFacility.Any(f => f.VoucherId == x.Id)
                || db.VoucherFacility.Any(f => f.VoucherId == x.Id && f.FacilityId == req.FacilityId)
                || db.ContractVoucher.Any(c => c.ContractId == req.Id && c.VoucherId == x.Id)
            )
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
                IsInUse = db.ContractVoucher.Any(c => c.VoucherId == x.Id) || db.WalletVoucherGrant.Any(g => g.VoucherId == x.Id),
            })
            .ToListAsync(ct);
        await Send.OkAsync(new AdminContractVouchersDTO { Vouchers = links, AvailableVouchers = vouchers }, ct);
    }
}
