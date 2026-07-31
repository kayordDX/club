using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ContractFieldConfigConfig : IEntityTypeConfiguration<ContractFieldConfig>
{
    public void Configure(EntityTypeBuilder<ContractFieldConfig> builder)
    {
        builder.Property(x => x.Created).HasDefaultValue(DateTime.MinValue);
    }
}
