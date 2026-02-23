using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class IdentityUserRoleConfig : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_role");

        builder.HasKey(ur => new { ur.UserId, ur.RoleId, ur.OutletId });

        builder.HasOne(ur => ur.Outlet)
            .WithMany()
            .HasForeignKey(ur => ur.OutletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ur => ur.OutletId);
    }
}
