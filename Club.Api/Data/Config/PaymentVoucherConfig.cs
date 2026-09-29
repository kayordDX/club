using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class PaymentVoucherConfig : IEntityTypeConfiguration<PaymentVoucher>
{
    public void Configure(EntityTypeBuilder<PaymentVoucher> builder)
    {
        builder.HasKey(x => x.PaymentId);
        builder.HasOne(x => x.Payment).WithOne().HasForeignKey<PaymentVoucher>(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.WalletVoucherGrant).WithMany().HasForeignKey(x => x.WalletVoucherGrantId).OnDelete(DeleteBehavior.Restrict);
    }
}
