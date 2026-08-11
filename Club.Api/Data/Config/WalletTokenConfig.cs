using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class WalletTokenConfig : IEntityTypeConfiguration<WalletToken>
{
    public void Configure(EntityTypeBuilder<WalletToken> builder)
    {
        builder.HasKey(x => new { x.WalletId, x.TokenTypeId });
    }
}
