using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class RoleTypeConfig : IEntityTypeConfiguration<RoleType>
{
    public void Configure(EntityTypeBuilder<RoleType> builder)
    {
        builder.Property(rt => rt.Name).IsRequired();
        builder.Property(rt => rt.Description).IsRequired();
    }
}
