using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Online.Data.Config;

public class IdentityUserPasskeyConfig : IEntityTypeConfiguration<IdentityUserPasskey<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserPasskey<Guid>> builder)
    {
        builder.ToTable("user_passkey");
    }
}