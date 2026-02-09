using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class BillConfig : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.HasOne(b => b.Payment).WithMany(p => p.Bills).HasForeignKey(b => b.PaymentId);
        builder.HasOne(b => b.PaymentType).WithMany(pt => pt.Bills).HasForeignKey(b => b.PaymentTypeId);
    }
}
