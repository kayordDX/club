using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenIddict.EntityFrameworkCore.Models;

namespace Online.Data.Config;

public class OpenIddictTokenConfig : IEntityTypeConfiguration<OpenIddictEntityFrameworkCoreToken>
{
    public void Configure(EntityTypeBuilder<OpenIddictEntityFrameworkCoreToken> builder)
    {
        builder.ToTable("openiddict_token");
    }
}
