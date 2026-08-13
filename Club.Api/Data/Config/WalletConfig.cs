using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class WalletConfig : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasIndex(x => x.UserId).IsUnique();

        builder
            .HasOne(x => x.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_wallet_users_user_id");
    }
}
