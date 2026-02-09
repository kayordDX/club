using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class ContractContractConfigConfig : IEntityTypeConfiguration<ContractContractConfig>
{
    public void Configure(EntityTypeBuilder<ContractContractConfig> builder)
    {
        builder.Property(x => x.Created)
            .HasDefaultValue(DateTime.MinValue);
    }
}
