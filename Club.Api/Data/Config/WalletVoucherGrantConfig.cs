using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class WalletVoucherGrantConfig : IEntityTypeConfiguration<WalletVoucherGrant>
{
    public void Configure(EntityTypeBuilder<WalletVoucherGrant> builder)
    {
        builder.HasIndex(x => x.WalletId);
        builder.HasIndex(x => x.UserContractId);
        builder.HasIndex(x => x.VoucherId);

        builder
            .HasOne(x => x.Wallet)
            .WithMany(w => w.VoucherGrants)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_wallet_voucher_grant_wallet_wallet_id");

        builder
            .HasOne(x => x.UserContract)
            .WithMany(c => c.VoucherGrants)
            .HasForeignKey(x => x.UserContractId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_wallet_voucher_grant_user_contract_user_contract_id");

        builder
            .HasOne(x => x.Voucher)
            .WithMany()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_wallet_voucher_grant_voucher_voucher_id");
    }
}
