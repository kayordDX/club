using Club.Common;
using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Voucher.Update;

public class Endpoint(AppDbContext db) : Endpoint<AdminVoucherUpdateRequest>
{
    public override void Configure()
    {
        Put("/admin/facility/{FacilityId}/voucher/{Id}");
        Description(x => x.WithName("AdminVoucherUpdate"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminVoucherUpdateRequest req, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var voucher = await db.Voucher.FromSqlInterpolated($"SELECT * FROM voucher WHERE id = {req.Id} FOR UPDATE").FirstOrDefaultAsync(ct);
        if (voucher is null || !await db.VoucherFacility.AnyAsync(x => x.VoucherId == req.Id && x.FacilityId == req.FacilityId, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (await db.VoucherFacility.AnyAsync(x => x.VoucherId == req.Id && x.FacilityId != req.FacilityId, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var inUse = await db.WalletVoucherGrant.AnyAsync(x => x.VoucherId == req.Id, ct) || await db.ContractVoucher.AnyAsync(x => x.VoucherId == req.Id, ct);
        var benefitsChanged =
            voucher.IsExtra != req.IsExtra
            || voucher.RedemptionKind != req.RedemptionKind
            || voucher.DiscountMode != req.DiscountMode
            || voucher.DiscountValue != req.DiscountValue
            || voucher.MaxDiscountAmount != req.MaxDiscountAmount;
        if (inUse && benefitsChanged)
        {
            AddError(nameof(req.IsExtra), "Only name and description can change after a voucher has been issued or added to a contract.");
            await Send.ErrorsAsync(409, ct);
            return;
        }
        voucher.Name = req.Name.Trim();
        voucher.Description = req.Description ?? string.Empty;
        if (!inUse)
        {
            voucher.IsExtra = req.IsExtra;
            voucher.RedemptionKind = req.RedemptionKind;
            voucher.DiscountMode = req.DiscountMode;
            voucher.DiscountValue = req.DiscountValue;
            voucher.MaxDiscountAmount = req.MaxDiscountAmount;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
