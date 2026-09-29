using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class VoucherExtraConfig : IEntityTypeConfiguration<VoucherExtra>
{
    public void Configure(EntityTypeBuilder<VoucherExtra> builder)
    {
        builder.HasKey(x => new { x.VoucherId, x.ExtraId });
        builder.HasOne(x => x.Voucher).WithMany().HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Extra).WithMany().HasForeignKey(x => x.ExtraId).OnDelete(DeleteBehavior.Restrict);
    }
}
