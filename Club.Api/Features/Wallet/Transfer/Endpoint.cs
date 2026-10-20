using Club.Common;
using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Wallet.Transfer;

public class Endpoint(AppDbContext db) : Endpoint<WalletTransferRequest>
{
    public override void Configure()
    {
        Post("/wallet/vouchers/{GrantId}/transfer");
        Description(x => x.WithName("WalletTransfer"));
    }

    public override async Task HandleAsync(WalletTransferRequest req, CancellationToken ct)
    {
        var senderId = Helpers.GetCurrentUserId(HttpContext);
        if (!senderId.HasValue)
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
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // The redemption endpoint takes the same lock, so ownership and balance cannot race a payment.
        var grant = await db
            .WalletVoucherGrant.FromSqlInterpolated($"SELECT * FROM wallet_voucher_grant WHERE id = {req.GrantId} FOR UPDATE")
            .FirstOrDefaultAsync(ct);
        if (grant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await db.Entry(grant).Reference(x => x.Wallet).LoadAsync(ct);
        if (grant.Wallet.UserId != senderId.Value)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (await db.WalletVoucherGrantAudit.AnyAsync(x => x.WalletVoucherGrantId == grant.Id && x.Action == WalletVoucherGrantAction.Transferred, ct))
        {
            AddError("This voucher has already been gifted and cannot be transferred again.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        if (!grant.Wallet.IsActive || grant.Wallet.Currency != "ZAR" || grant.AmountRemaining <= 0 || grant.ExpiryDate <= DateTime.UtcNow)
        {
            AddError("Only unexpired vouchers with a remaining balance in an active ZAR wallet can be gifted.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var recipient = req.Recipient.Trim();
        // Use the same recipient lock as admin issuance to serialize wallet creation.
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
        if (user.Id == senderId.Value)
        {
            AddError("You cannot gift a voucher to yourself.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var wallet = await db.Wallet.SingleOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (wallet is not null && (!wallet.IsActive || wallet.Currency != grant.Wallet.Currency))
        {
            AddError("The recipient's wallet must be active and use ZAR.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        if (wallet is null)
        {
            wallet = new Club.Entities.Wallet
            {
                Id = Guid.NewGuid(),
                User = user,
                Currency = grant.Wallet.Currency,
            };
            db.Wallet.Add(wallet);
        }
        grant.Wallet = wallet;
        db.WalletVoucherGrantAudit.Add(
            new WalletVoucherGrantAudit
            {
                Id = Guid.NewGuid(),
                WalletVoucherGrant = grant,
                Action = WalletVoucherGrantAction.Transferred,
                Timestamp = DateTime.UtcNow,
                SourceType = WalletVoucherGrantSource.Gift,
                AssigningUserId = senderId.Value,
                RecipientUserId = user.Id,
            }
        );
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
