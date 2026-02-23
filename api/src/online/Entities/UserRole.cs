using Microsoft.AspNetCore.Identity;

namespace Online.Entities;

public class UserRole : IdentityUserRole<Guid>
{
    public int? OutletId { get; set; }
    public Outlet? Outlet { get; set; } = default!;
    public User User { get; set; } = default!;
}
