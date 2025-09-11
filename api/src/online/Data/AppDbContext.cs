using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Online.Entities;

namespace Online.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().ToTable("user");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("role");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("claim");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_role");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_login");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claim");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_token");
    }
}