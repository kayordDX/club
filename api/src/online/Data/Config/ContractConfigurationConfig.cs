using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class ContractConfigurationConfig : IEntityTypeConfiguration<ContractConfig>
{
    public void Configure(EntityTypeBuilder<ContractConfig> builder)
    {
        builder.Property(cc => cc.Name)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(cc => cc.FieldValidation)
            .HasMaxLength(250);

        builder.Property(cc => cc.Created)
            .HasDefaultValue(DateTime.MinValue);
    }
}
