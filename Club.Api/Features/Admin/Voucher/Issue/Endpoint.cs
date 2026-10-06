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
        if (string.IsNullOrWhiteSpace(req.Recipient) || req.Recipient.Length > 320)
        {
            AddError("Enter the recipient's full email address or phone number.");
            await Send.ErrorsAsync(400, ct);
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
        if (voucher is null)
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
        var recipient = req.Recipient.Trim();
        // Lock the recipient so concurrent sends cannot create two wallets for the same user.
        var users = await db
            .Users.FromSqlInterpolated($"SELECT * FROM \"user\" WHERE lower(email) = {recipient.ToLowerInvariant()} OR phone_number = {recipient} FOR UPDATE")
            .Take(2)
            .ToListAsync(ct);
        if (users.Count != 1)
        {
            AddError(
                users.Count == 0
                    ? "No user found with that email address or phone number."
                    : "More than one user matches. Use a unique email address or phone number."
            );
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var user = users[0];
        var wallet = await db.Wallet.SingleOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (wallet is not null && (!wallet.IsActive || wallet.Currency != "ZAR"))
        {
            AddError("The recipient's wallet must be active and use ZAR.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        wallet ??= new Club.Entities.Wallet
        {
            Id = Guid.NewGuid(),
            User = user,
            Currency = "ZAR",
        };
        if (
            req.SourceUserContractId.HasValue
            && !await db.UserContract.AnyAsync(
                x =>
                    x.Id == req.SourceUserContractId
                    && x.UserId == user.Id
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
        // Create any new wallet, grant and audit together.
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await Send.OkAsync(grant.Id, ct);
    }
}
