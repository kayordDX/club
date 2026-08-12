using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class WalletTransactionConfig : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasIndex(x => x.WalletId);

        builder
            .HasOne(x => x.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_wallet_transaction_wallet_wallet_id");

        builder
            .HasOne(x => x.WalletTransactionType)
            .WithMany()
            .HasForeignKey(x => x.WalletTransactionTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_wallet_transaction_wallet_transaction_type_wallet_transaction_type_id");
    }
}
