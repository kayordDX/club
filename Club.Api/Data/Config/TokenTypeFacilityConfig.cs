using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class TokenTypeFacilityConfig : IEntityTypeConfiguration<TokenTypeFacility>
{
    public void Configure(EntityTypeBuilder<TokenTypeFacility> builder)
    {
        builder.HasKey(x => new { x.TokenTypeId, x.FacilityId });
    }
}
