using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class ContractFieldConfigConfig : IEntityTypeConfiguration<ContractFieldConfig>
{
    public void Configure(EntityTypeBuilder<ContractFieldConfig> builder)
    {
        builder.Property(x => x.Created)
            .HasDefaultValue(DateTime.MinValue);
    }
}
