using Club.Common;
using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Voucher.Issue;

public class Endpoint(AppDbContext db) : Endpoint<AdminVoucherIssueRequest, Guid>
{
    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/voucher/issue");
        Description(x => x.WithName("AdminVoucherIssue"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminVoucherIssueRequest req, CancellationToken ct)
    {
        var actor = Helpers.GetCurrentUserId(HttpContext);
        if (!actor.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        if (
            req.Amount <= 0
            || req.ValidFrom.Kind != DateTimeKind.Utc
            || req.ExpiryDate.Kind != DateTimeKind.Utc
            || req.ExpiryDate <= req.ValidFrom
            || req.ExpiryDate <= DateTime.UtcNow
            || req.Reason?.Length > 1000
            || req.Reference?.Length > 200
        )
        {
            AddError("Provide a positive amount, UTC validity dates and a future expiry; reason/reference limits are 1000/200 characters.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var wallet = await db.Wallet.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == req.WalletId && x.IsActive && x.Currency == "ZAR", ct);
        // Serialize issuance with benefit updates and voucher deletion.
        var voucher = await db.Voucher.FromSqlInterpolated($"SELECT * FROM voucher WHERE id = {req.VoucherId} FOR UPDATE").FirstOrDefaultAsync(ct);
        if (
            voucher is not null
            && (
                !await db.VoucherFacility.AnyAsync(f => f.VoucherId == voucher.Id && f.FacilityId == req.FacilityId, ct)
                || await db.VoucherFacility.AnyAsync(f => f.VoucherId == voucher.Id && f.FacilityId != req.FacilityId, ct)
            )
        )
            voucher = null;
        if (wallet is null || voucher is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (
            (voucher.RedemptionKind != VoucherRedemptionKind.Credit && decimal.Truncate(req.Amount) != req.Amount)
            || (voucher.RedemptionKind == VoucherRedemptionKind.Credit && decimal.Round(req.Amount, 2) != req.Amount)
        )
        {
            AddError("Entitlements/discounts require whole units; credit requires at most two decimal places.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        if (
            req.SourceUserContractId.HasValue
            && !await db.UserContract.AnyAsync(
                x =>
                    x.Id == req.SourceUserContractId
                    && x.UserId == wallet.UserId
                    && x.IsActive
                    && x.StartDate <= DateTime.UtcNow
                    && (!x.EndDate.HasValue || x.EndDate > DateTime.UtcNow)
                    && x.Contract.ContractFacilities.Any(f => f.FacilityId == req.FacilityId)
                    && db.ContractVoucher.Any(v => v.ContractId == x.ContractId && v.VoucherId == voucher.Id),
                ct
            )
        )
        {
            AddError("Source membership must be active, belong to the wallet owner and grant this voucher at this facility.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var grant = new WalletVoucherGrant
        {
            Id = Guid.NewGuid(),
            Wallet = wallet,
            Voucher = voucher,
            AmountGranted = req.Amount,
            AmountRemaining = req.Amount,
            GrantedAt = req.ValidFrom,
            ExpiryDate = req.ExpiryDate,
        };
        db.WalletVoucherGrant.Add(grant);
        db.WalletVoucherGrantAudit.Add(
            new WalletVoucherGrantAudit
            {
                Id = Guid.NewGuid(),
                WalletVoucherGrant = grant,
                Action = WalletVoucherGrantAction.Issued,
                Timestamp = DateTime.UtcNow,
                SourceType = req.SourceUserContractId.HasValue ? WalletVoucherGrantSource.Contract : WalletVoucherGrantSource.Admin,
                AssigningUserId = actor.Value,
                SourceUserContractId = req.SourceUserContractId,
                Reason = req.Reason,
                Reference = req.Reference,
            }
        );
        // EF commits both inserts in one transaction, including failures of either insert.
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await Send.OkAsync(grant.Id, ct);
    }
}
