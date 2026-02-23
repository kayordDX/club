using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class IdentityUserRoleConfig : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_role");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.Id)
            .ValueGeneratedOnAdd();

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId, ur.OutletId })
            .IsUnique();

        builder.HasIndex(ur => ur.OutletId);

        builder
            .HasOne<User>()
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne<IdentityRole<Guid>>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(ur => ur.Outlet)
            .WithMany()
            .HasForeignKey(ur => ur.OutletId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
