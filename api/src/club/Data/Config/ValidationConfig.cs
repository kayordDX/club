using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class ValidationConfig : IEntityTypeConfiguration<Validation>
{
    public void Configure(EntityTypeBuilder<Validation> builder)
    {
        builder.Property(v => v.Name)
            .HasMaxLength(250)
            .IsRequired();
    }
}
