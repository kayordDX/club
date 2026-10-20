using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract;

public static class ContractMemberAssignment
{
    public static async Task<string?> SaveAsync(AppDbContext db, UserContract membership, Guid? actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize benefit edits with activation, and wallet creation with other issuance.
        await db.Contract.FromSqlInterpolated($"SELECT * FROM contract WHERE id = {membership.ContractId} FOR UPDATE").SingleAsync(ct);
        await db.Users.FromSqlInterpolated($"SELECT * FROM \"user\" WHERE id = {membership.UserId} FOR UPDATE").SingleAsync(ct);
        if (await db.UserContract.AnyAsync(x => x.UserId == membership.UserId && x.ContractId == membership.ContractId, ct))
            return "This user is already a member of the contract.";

        var allowances = await db.ContractVoucher.Include(x => x.Voucher).Where(x => x.ContractId == membership.ContractId).ToListAsync(ct);
        var wallet = allowances.Count == 0 ? null : await db.Wallet.SingleOrDefaultAsync(x => x.UserId == membership.UserId, ct);
        if (wallet is not null && (!wallet.IsActive || wallet.Currency != "ZAR"))
            return "The member's wallet must be active and use ZAR.";
        if (allowances.Count > 0 && (!membership.EndDate.HasValue || membership.EndDate <= membership.StartDate))
            return "Voucher benefits require a membership expiry after the start date.";

        db.UserContract.Add(membership);
        // Obtain the membership ID for immutable grant provenance, within the same transaction.
        await db.SaveChangesAsync(ct);
        if (allowances.Count > 0)
        {
            wallet ??= new Entities.Wallet
            {
                Id = Guid.NewGuid(),
                User = membership.User,
                Currency = "ZAR",
            };
            foreach (var allowance in allowances)
            {
                var grant = new WalletVoucherGrant
                {
                    Id = Guid.NewGuid(),
                    Wallet = wallet,
                    Voucher = allowance.Voucher,
                    AmountGranted = allowance.Amount,
                    AmountRemaining = allowance.Amount,
                    GrantedAt = membership.StartDate,
                    ExpiryDate = membership.EndDate!.Value,
                };
                db.WalletVoucherGrantAudit.Add(
                    new WalletVoucherGrantAudit
                    {
                        Id = Guid.NewGuid(),
                        WalletVoucherGrant = grant,
                        Action = WalletVoucherGrantAction.Issued,
                        Timestamp = DateTime.UtcNow,
                        SourceType = WalletVoucherGrantSource.Contract,
                        AssigningUserId = actorId,
                        SourceUserContractId = membership.Id,
                    }
                );
            }
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return null;
    }
}
