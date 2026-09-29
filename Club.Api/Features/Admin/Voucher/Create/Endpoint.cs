using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Voucher.Create;

public class Endpoint(AppDbContext db) : Endpoint<AdminVoucherCreateRequest, int>
{
    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/voucher");
        Description(x => x.WithName("AdminVoucherCreate"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminVoucherCreateRequest req, CancellationToken ct)
    {
        if (!await db.Facility.AnyAsync(x => x.Id == req.FacilityId, ct))
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
        var voucher = new Entities.Voucher { Name = req.Name };
        VoucherAdmin.Apply(voucher, req);
        db.Voucher.Add(voucher);
        db.VoucherFacility.Add(
            new VoucherFacility
            {
                Voucher = voucher,
                FacilityId = req.FacilityId,
                Facility = null!,
            }
        );
        await VoucherAdmin.SetItemsAsync(db, voucher, req, ct);
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(voucher.Id, ct);
    }
}
