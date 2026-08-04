using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class OutletConfig : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.HasIndex(o => o.Slug).IsUnique();

        // Stored generated full-text search vector over the user-facing text fields.
        // Uses snake_case column names (EFCore.NamingConventions) in the raw SQL.
        builder
            .Property(o => o.SearchVector)
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('english', coalesce(name, '') || ' ' || coalesce(display_name, '') || ' ' || coalesce(description, '') || ' ' || coalesce(address, '') || ' ' || coalesce(tags, ''))",
                stored: true
            );

        // GIN index keeps full-text search fast as the outlet table grows.
        builder.HasIndex(o => o.SearchVector).HasMethod("gin");
    }
}
