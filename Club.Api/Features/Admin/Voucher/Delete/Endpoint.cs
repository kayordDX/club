using Club.Common;
using Club.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Club.Features.Admin.Voucher.Delete;

public class Endpoint(AppDbContext db) : Endpoint<AdminVoucherDeleteRequest>
{
    public override void Configure()
    {
        Delete("/admin/facility/{FacilityId}/voucher/{Id}");
        Description(x => x.WithName("AdminVoucherDelete"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminVoucherDeleteRequest req, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var voucher = await db.Voucher.FromSqlInterpolated($"SELECT * FROM voucher WHERE id = {req.Id} FOR UPDATE").FirstOrDefaultAsync(ct);
        if (
            voucher is null
            || !await db.VoucherFacility.AnyAsync(x => x.VoucherId == req.Id && x.FacilityId == req.FacilityId, ct)
            || await db.VoucherFacility.AnyAsync(x => x.VoucherId == req.Id && x.FacilityId != req.FacilityId, ct)
        )
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (await db.WalletVoucherGrant.AnyAsync(x => x.VoucherId == req.Id, ct) || await db.ContractVoucher.AnyAsync(x => x.VoucherId == req.Id, ct))
        {
            AddError(nameof(req.Id), "This voucher cannot be deleted because it has issued grants or contract definitions.");
            await Send.ErrorsAsync(409, ct);
            return;
        }
        db.Voucher.Remove(voucher);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
        {
            AddError(nameof(req.Id), "This voucher cannot be deleted because it is in use.");
            await Send.ErrorsAsync(409, ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
