using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Online.Entities;

namespace Online.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    // Existing
    public DbSet<Resource> Resource { get; set; }
    public DbSet<UserRefreshToken> UserRefreshToken { get; set; }
    public DbSet<Facility> Facility { get; set; }
    public DbSet<Slot> Slot { get; set; }

    // New Business Entities
    public DbSet<Business> Business { get; set; }
    public DbSet<Outlet> Outlet { get; set; }
    public DbSet<OutletType> OutletType { get; set; }

    // Contract Entities
    public DbSet<Contract> Contract { get; set; }
    public DbSet<ContractConfig> ContractConfig { get; set; }
    public DbSet<ContractOutlet> ContractOutlet { get; set; }
    public DbSet<ContractContractConfig> ContractContractConfig { get; set; }

    // Booking Entities
    public DbSet<BookingStatus> BookingStatus { get; set; }

    // Payment Entities
    public DbSet<Payment> Payment { get; set; }
    public DbSet<PaymentStatus> PaymentStatus { get; set; }
    public DbSet<PaymentType> PaymentType { get; set; }
    public DbSet<Bill> Bill { get; set; }

    // Slot Entities
    public DbSet<SlotContract> SlotContract { get; set; }
    public DbSet<SlotBooking> SlotBooking { get; set; }
    public DbSet<SlotBooking> BookingSlot { get; set; }

    // Extra Entities
    public DbSet<Extra> Extra { get; set; }
    public DbSet<FacilityExtra> FacilityExtra { get; set; }
    public DbSet<ExtraBooking> ExtraBooking { get; set; }

    // User Entities
    public DbSet<UserContract> UserContract { get; set; }

    // Validation Entities
    public DbSet<Validation> Validation { get; set; }
    public DbSet<RoleType> RoleType { get; set; }
}
