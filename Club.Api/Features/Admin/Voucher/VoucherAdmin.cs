using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Admin.Voucher.Create;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Voucher;

public static class VoucherAdmin
{
    public static async Task<string?> ValidateAsync(AppDbContext db, AdminVoucherCreateRequest req, CancellationToken ct)
    {
        if (
            string.IsNullOrWhiteSpace(req.Name)
            || req.Name.Length > 200
            || req.Description is null
            || req.ContractIds is null
            || req.ExtraIds is null
            || !Enum.IsDefined(req.RedemptionKind)
        )
            return "Provide a name (up to 200 characters), description, redemption kind and item lists.";
        if (
            req.RedemptionKind == VoucherRedemptionKind.Discount
            && (
                !req.DiscountMode.HasValue
                || !Enum.IsDefined(req.DiscountMode.Value)
                || !req.DiscountValue.HasValue
                || req.DiscountValue <= 0
                || (req.DiscountMode == VoucherDiscountMode.Percentage && req.DiscountValue > 100)
                || req.MaxDiscountAmount <= 0
            )
        )
            return "Provide a valid discount mode/value and optional positive cap.";
        var contracts = req.ContractIds.Distinct().ToList();
        var extras = req.ExtraIds.Distinct().ToList();
        if (req.RedemptionKind == VoucherRedemptionKind.Entitlement && ((req.IsExtra && contracts.Count > 0) || (!req.IsExtra && extras.Count > 0)))
            return "Round entitlements select contracts; extra entitlements select extras.";
        if (
            await db.ContractFacility.CountAsync(x => x.FacilityId == req.FacilityId && contracts.Contains(x.ContractId), ct) != contracts.Count
            || await db.Extra.CountAsync(x => x.FacilityId == req.FacilityId && extras.Contains(x.Id), ct) != extras.Count
        )
            return "All selected contracts and extras must belong to this facility.";
        return null;
    }

    public static void Apply(Entities.Voucher voucher, AdminVoucherCreateRequest req)
    {
        voucher.Name = req.Name.Trim();
        voucher.Description = req.Description;
        voucher.IsExtra = req.IsExtra;
        voucher.RedemptionKind = req.RedemptionKind;
        voucher.DiscountMode = req.RedemptionKind == VoucherRedemptionKind.Discount ? req.DiscountMode : null;
        voucher.DiscountValue = req.RedemptionKind == VoucherRedemptionKind.Discount ? req.DiscountValue : null;
        voucher.MaxDiscountAmount = req.RedemptionKind == VoucherRedemptionKind.Discount ? req.MaxDiscountAmount : null;
    }

    public static async Task SetItemsAsync(AppDbContext db, Entities.Voucher voucher, AdminVoucherCreateRequest req, CancellationToken ct)
    {
        var contracts = await db.VoucherContract.Where(x => x.VoucherId == voucher.Id).ToListAsync(ct);
        var extras = await db.VoucherExtra.Where(x => x.VoucherId == voucher.Id).ToListAsync(ct);
        db.VoucherContract.RemoveRange(contracts.Where(x => !req.ContractIds.Contains(x.ContractId)));
        db.VoucherExtra.RemoveRange(extras.Where(x => !req.ExtraIds.Contains(x.ExtraId)));
        foreach (var id in req.ContractIds.Distinct().Except(contracts.Select(x => x.ContractId)))
            db.VoucherContract.Add(
                new VoucherContract
                {
                    Voucher = voucher,
                    ContractId = id,
                    Contract = null!,
                }
            );
        foreach (var id in req.ExtraIds.Distinct().Except(extras.Select(x => x.ExtraId)))
            db.VoucherExtra.Add(
                new VoucherExtra
                {
                    Voucher = voucher,
                    ExtraId = id,
                    Extra = null!,
                }
            );
    }
}
