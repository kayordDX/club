using Club.Data;
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
        var voucher = await db.Voucher.FirstOrDefaultAsync(
            x =>
                x.Id == req.Id
                && db.VoucherFacility.Any(f => f.VoucherId == x.Id && f.FacilityId == req.FacilityId)
                && !db.VoucherFacility.Any(f => f.VoucherId == x.Id && f.FacilityId != req.FacilityId),
            ct
        );
        if (voucher is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var error = await VoucherAdmin.ValidateAsync(db, req, ct);
        if (error is not null)
        {
            AddError(error);
            await Send.ErrorsAsync(400, ct);
            return;
        }
        VoucherAdmin.Apply(voucher, req);
        await VoucherAdmin.SetItemsAsync(db, voucher, req, ct);
        await db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
