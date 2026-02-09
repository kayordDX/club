using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class FacilityExtraConfig : IEntityTypeConfiguration<FacilityExtra>
{
    public void Configure(EntityTypeBuilder<FacilityExtra> builder)
    {
        builder.Property(fe => fe.Name).HasMaxLength(250);
        builder.Property(fe => fe.Code).HasMaxLength(250);
        builder.Property(fe => fe.Created).HasDefaultValue(DateTime.MinValue);
        builder.Property(fe => fe.StartDate).IsRequired();
    }
}
