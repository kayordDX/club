using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Online.Entities;

namespace Online.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        // modelBuilder.Entity<User>().ToTable("user");
        // modelBuilder.Entity<IdentityRole<int>>().ToTable("role");
        // modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("claim");
        // modelBuilder.Entity<IdentityUserRole<int>>().ToTable("user_role");
        // modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("user_login");
        // modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("role_claim");
        // modelBuilder.Entity<IdentityUserToken<int>>().ToTable("user_token");
    }
}