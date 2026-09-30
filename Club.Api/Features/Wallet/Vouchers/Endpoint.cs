using Club.Common;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Wallet.Vouchers;

public class Endpoint(AppDbContext db) : EndpointWithoutRequest<List<WalletVoucherDTO>>
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

        var grants = await db
            .WalletVoucherGrant.AsNoTracking()
            .Where(x => x.Wallet.UserId == userId.Value)
            .OrderBy(x => x.ExpiryDate)
            .ThenBy(x => x.Id)
            .Select(x => new WalletVoucherDTO
            {
                GrantId = x.Id,
                VoucherId = x.VoucherId,
                Name = x.Voucher.Name,
                Description = x.Voucher.Description,
                FacilityNames = db.VoucherFacility.Where(f => f.VoucherId == x.VoucherId).Select(f => f.Facility.Name).Distinct().Order().ToList(),
                GameTypes = db.VoucherFacility.Where(f => f.VoucherId == x.VoucherId).Select(f => f.Facility.FacilityType.Name).Distinct().Order().ToList(),
                ExtraNames = db
                    .Extra.Where(e => db.VoucherFacility.Any(f => f.VoucherId == x.VoucherId && f.FacilityId == e.FacilityId))
                    .Select(e => e.Name)
                    .Distinct()
                    .Order()
                    .ToList(),
                IsExtra = x.Voucher.IsExtra,
                RedemptionKind = x.Voucher.RedemptionKind,
                DiscountMode = x.Voucher.DiscountMode,
                DiscountValue = x.Voucher.DiscountValue,
                MaxDiscountAmount = x.Voucher.MaxDiscountAmount,
                AmountGranted = x.AmountGranted,
                AmountRemaining = x.AmountRemaining,
                GrantedAt = x.GrantedAt,
                ExpiryDate = x.ExpiryDate,
                Currency = x.Wallet.Currency,
                IsWalletActive = x.Wallet.IsActive,
            })
            .ToListAsync(ct);

        await Send.OkAsync(grants, ct);
    }
}
