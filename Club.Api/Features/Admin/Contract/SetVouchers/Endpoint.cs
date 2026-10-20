using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.SetVouchers;

public class Endpoint(AppDbContext db) : Endpoint<AdminContractSetVouchersRequest>
{
    public override void Configure()
    {
        Put("/admin/facility/{FacilityId}/contract/{Id}/vouchers");
        Description(x => x.WithName("AdminContractSetVouchers"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractSetVouchersRequest req, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var contract = await db.Contract.FromSqlInterpolated($"SELECT * FROM contract WHERE id = {req.Id} FOR UPDATE").FirstOrDefaultAsync(ct);
        if (contract is null || !await db.ContractFacility.AnyAsync(x => x.ContractId == req.Id && x.FacilityId == req.FacilityId, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (
            req.Vouchers is null
            || req.Vouchers.Count > 500
            || req.Vouchers.Any(x => x is null)
            || req.Vouchers.Select(x => x.VoucherId).Distinct().Count() != req.Vouchers.Count
        )
        {
            AddError("Choose each voucher only once (up to 500 vouchers).");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var ids = req.Vouchers.Select(x => x.VoucherId).Order().ToArray();
        // Coordinate with definition updates/deletion so a linked benefit cannot change underneath us.
        var vouchers = await db.Voucher.FromSqlInterpolated($"SELECT * FROM voucher WHERE id = ANY ({ids}) ORDER BY id FOR UPDATE").ToListAsync(ct);
        foreach (var link in req.Vouchers)
        {
            var voucher = vouchers.SingleOrDefault(x => x.Id == link.VoucherId);
            if (
                voucher is null
                || (
                    await db.VoucherFacility.AnyAsync(f => f.VoucherId == link.VoucherId, ct)
                    && !await db.VoucherFacility.AnyAsync(f => f.VoucherId == link.VoucherId && f.FacilityId == req.FacilityId, ct)
                )
            )
            {
                AddError("Choose vouchers valid at this facility.");
                await Send.ErrorsAsync(400, ct);
                return;
            }
            if (
                link.Amount <= 0
                || link.Amount > 9999999999999999.99m
                || (
                    voucher.RedemptionKind == VoucherRedemptionKind.Credit
                        ? decimal.Round(link.Amount, 2) != link.Amount
                        : decimal.Truncate(link.Amount) != link.Amount
                )
            )
            {
                AddError("Amounts must be positive: whole units/uses, or credit with at most two decimal places.");
                await Send.ErrorsAsync(400, ct);
                return;
            }
        }

        var existing = await db.ContractVoucher.Where(x => x.ContractId == req.Id).ToListAsync(ct);
        db.ContractVoucher.RemoveRange(existing.Where(x => !ids.Contains(x.VoucherId)));
        foreach (var link in req.Vouchers)
        {
            var current = existing.SingleOrDefault(x => x.VoucherId == link.VoucherId);
            if (current is not null)
                current.Amount = link.Amount;
            else
                db.ContractVoucher.Add(
                    new ContractVoucher
                    {
                        Contract = contract,
                        Voucher = vouchers.Single(x => x.Id == link.VoucherId),
                        Amount = link.Amount,
                    }
                );
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
