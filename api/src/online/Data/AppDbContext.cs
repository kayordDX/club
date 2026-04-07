using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Online.Entities;

namespace Online.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public DbSet<Booking> Booking { get; set; }
    public DbSet<BookingStatus> BookingStatus { get; set; }
    public DbSet<Business> Business { get; set; }
    public DbSet<Contract> Contract { get; set; }
    public DbSet<ContractField> ContractField { get; set; }
    public DbSet<ContractFieldConfig> ContractFieldConfig { get; set; }
    public DbSet<ContractOutlet> ContractOutlet { get; set; }
    public DbSet<Extra> Extra { get; set; }
    public DbSet<ExtraBooking> ExtraBooking { get; set; }
    public DbSet<Facility> Facility { get; set; }
    public DbSet<Outlet> Outlet { get; set; }
    public DbSet<OutletType> OutletType { get; set; }
    public DbSet<Payment> Payment { get; set; }
    public DbSet<PaymentBooking> PaymentBooking { get; set; }
    public DbSet<PaymentStatus> PaymentStatus { get; set; }
    public DbSet<PaymentType> PaymentType { get; set; }
    public DbSet<Resource> Resource { get; set; }
    public DbSet<RoleType> RoleType { get; set; }
    public DbSet<Slot> Slot { get; set; }
    public DbSet<SlotContract> SlotContract { get; set; }
    public DbSet<SlotContractBooking> SlotContractBooking { get; set; }
    public DbSet<UserContract> UserContract { get; set; }
    public DbSet<UserRefreshToken> UserRefreshToken { get; set; }
    public DbSet<Validation> Validation { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = new CancellationToken())
    {
        var userId = _httpContextAccessor.HttpContext?.User?.GetUserId();
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created = DateTime.UtcNow;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModified = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = userId;
                    break;
            }
        }
        int returnValue = await base.SaveChangesAsync(ct);
        return returnValue;
    }

}
