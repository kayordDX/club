using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class OutletTypeConfig : IEntityTypeConfiguration<OutletType>
{
    public void Configure(EntityTypeBuilder<OutletType> builder)
    {
        builder.Property(ot => ot.Name).HasMaxLength(250);
    }
}
