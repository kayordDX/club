using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class WalletBalanceConfig : IEntityTypeConfiguration<WalletBalance>
{
    public void Configure(EntityTypeBuilder<WalletBalance> builder)
    {
        builder.HasKey(x => x.WalletId);

        builder
            .HasOne(x => x.Wallet)
            .WithOne(w => w.Balance)
            .HasForeignKey<WalletBalance>(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_wallet_balance_wallet_wallet_id");
    }
}
