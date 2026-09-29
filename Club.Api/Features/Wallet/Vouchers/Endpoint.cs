using Club.Common;
using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Wallet.Vouchers;

public class Endpoint(AppDbContext db) : EndpointWithoutRequest<List<AvailableVoucherDTO>>
{
    public override void Configure()
    {
        Get("/wallet/vouchers");
        Description(x => x.WithName("WalletVouchers"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (!userId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var now = DateTime.UtcNow;
        var results = await db
            .WalletVoucherGrant.Where(g =>
                g.Wallet.UserId == userId.Value
                && g.Wallet.IsActive
                && g.Wallet.Currency == "ZAR"
                && g.GrantedAt <= now
                && g.ExpiryDate > now
                && g.AmountRemaining > 0
                && (g.Voucher.RedemptionKind == VoucherRedemptionKind.Credit || g.AmountRemaining >= 1)
                && db.VoucherFacility.Any(f => f.VoucherId == g.VoucherId)
                && (
                    g.Voucher.RedemptionKind != VoucherRedemptionKind.Entitlement
                    || (
                        g.Voucher.IsExtra
                            ? db.VoucherExtra.Any(e =>
                                e.VoucherId == g.VoucherId && db.VoucherFacility.Any(f => f.VoucherId == g.VoucherId && f.FacilityId == e.Extra.FacilityId)
                            )
                            : db.VoucherContract.Any(c => c.VoucherId == g.VoucherId)
                    )
                )
            )
            .OrderBy(g => g.ExpiryDate)
            .ThenBy(g => g.Id)
            .Select(g => new AvailableVoucherDTO
            {
                GrantId = g.Id,
                VoucherId = g.VoucherId,
                Name = g.Voucher.Name,
                Description = g.Voucher.Description,
                IsExtra = g.Voucher.IsExtra,
                RedemptionKind = g.Voucher.RedemptionKind,
                DiscountMode = g.Voucher.DiscountMode,
                DiscountValue = g.Voucher.DiscountValue,
                MaxDiscountAmount = g.Voucher.MaxDiscountAmount,
                AmountRemaining = g.AmountRemaining,
                GrantedAt = g.GrantedAt,
                ExpiryDate = g.ExpiryDate,
                FacilityIds = db.VoucherFacility.Where(f => f.VoucherId == g.VoucherId).OrderBy(f => f.FacilityId).Select(f => f.FacilityId).ToList(),
                ContractIds = db.VoucherContract.Where(c => c.VoucherId == g.VoucherId).OrderBy(c => c.ContractId).Select(c => c.ContractId).ToList(),
                ExtraIds = db.VoucherExtra.Where(e => e.VoucherId == g.VoucherId).OrderBy(e => e.ExtraId).Select(e => e.ExtraId).ToList(),
            })
            .ToListAsync(ct);
        await Send.OkAsync(results, ct);
    }
}
