using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ContractTokenConfig : IEntityTypeConfiguration<ContractToken>
{
    public void Configure(EntityTypeBuilder<ContractToken> builder)
    {
        builder.HasKey(x => new { x.ContractId, x.TokenId });
    }
}
