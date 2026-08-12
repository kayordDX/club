using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ContractVoucherConfig : IEntityTypeConfiguration<ContractVoucher>
{
    public void Configure(EntityTypeBuilder<ContractVoucher> builder)
    {
        builder.HasKey(x => new { x.ContractId, x.VoucherId });

        builder.Property(x => x.Amount).HasDefaultValue(1);

        builder
            .HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_contract_voucher_contract_contract_id");

        builder
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_contract_voucher_voucher_voucher_id");
    }
}
