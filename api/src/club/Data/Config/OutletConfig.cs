using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Club.Entities;

namespace Club.Data.Config;

public class OutletConfig : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.HasIndex(o => o.Slug).IsUnique();
    }
}
