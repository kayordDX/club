using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Voucher.GetAll;

public class AdminVoucherGetAllRequest
{
    public int FacilityId { get; set; }
}

public class Endpoint(AppDbContext db) : Endpoint<AdminVoucherGetAllRequest, List<AdminVoucherDTO>>
{
    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/voucher");
        Description(x => x.WithName("AdminVoucherGetAll"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminVoucherGetAllRequest req, CancellationToken ct)
    {
        var results = await db
            .Voucher.Where(x => db.VoucherFacility.Any(f => f.VoucherId == x.Id && f.FacilityId == req.FacilityId))
            .OrderBy(x => x.Name)
            .Select(x => new AdminVoucherDTO
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsExtra = x.IsExtra,
                RedemptionKind = x.RedemptionKind,
                DiscountMode = x.DiscountMode,
                DiscountValue = x.DiscountValue,
                MaxDiscountAmount = x.MaxDiscountAmount,
                CanEdit = !db.VoucherFacility.Any(f => f.VoucherId == x.Id && f.FacilityId != req.FacilityId),
                ContractIds = db.VoucherContract.Where(c => c.VoucherId == x.Id).OrderBy(c => c.ContractId).Select(c => c.ContractId).ToList(),
                ExtraIds = db.VoucherExtra.Where(e => e.VoucherId == x.Id).OrderBy(e => e.ExtraId).Select(e => e.ExtraId).ToList(),
            })
            .ToListAsync(ct);
        await Send.OkAsync(results, ct);
    }
}
