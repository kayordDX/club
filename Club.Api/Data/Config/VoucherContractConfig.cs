using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class VoucherContractConfig : IEntityTypeConfiguration<VoucherContract>
{
    public void Configure(EntityTypeBuilder<VoucherContract> builder)
    {
        builder.HasKey(x => new { x.VoucherId, x.ContractId });
        builder.HasOne(x => x.Voucher).WithMany().HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
    }
}
