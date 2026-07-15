using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Club.Entities;

namespace Club.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public DbSet<Booking> Booking => Set<Booking>();
    public DbSet<BookingStatus> BookingStatus => Set<BookingStatus>();
    public DbSet<Business> Business => Set<Business>();
    public DbSet<Contract> Contract => Set<Contract>();
    public DbSet<ContractField> ContractField => Set<ContractField>();
    public DbSet<ContractFieldConfig> ContractFieldConfig => Set<ContractFieldConfig>();
    public DbSet<ContractOutlet> ContractOutlet => Set<ContractOutlet>();
    public DbSet<EmailLog> EmailLog => Set<EmailLog>();
    public DbSet<Extra> Extra => Set<Extra>();
    public DbSet<ExtraBooking> ExtraBooking => Set<ExtraBooking>();
    public DbSet<Facility> Facility => Set<Facility>();
    public DbSet<Outlet> Outlet => Set<Outlet>();
    public DbSet<OutletType> OutletType => Set<OutletType>();
    public DbSet<Payment> Payment => Set<Payment>();
    public DbSet<PaymentBooking> PaymentBooking => Set<PaymentBooking>();
    public DbSet<PaymentProviderConfig> PaymentProviderConfig => Set<PaymentProviderConfig>();
    public DbSet<PaymentStatus> PaymentStatus => Set<PaymentStatus>();
    public DbSet<PaymentType> PaymentType => Set<PaymentType>();
    public DbSet<Resource> Resource => Set<Resource>();
    public DbSet<Slot> Slot => Set<Slot>();
    public DbSet<SlotContract> SlotContract => Set<SlotContract>();
    public DbSet<SlotContractBooking> SlotContractBooking => Set<SlotContractBooking>();
    public DbSet<UserContract> UserContract => Set<UserContract>();
    public DbSet<Validation> Validation => Set<Validation>();

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
