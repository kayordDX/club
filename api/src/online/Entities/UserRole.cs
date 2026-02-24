using Microsoft.AspNetCore.Identity;

namespace Online.Entities;

public class UserRole : IdentityUserRole<Guid>
{
    public int Id { get; set; }
    public int? OutletId { get; set; }
    public Outlet? Outlet { get; set; }
    public Role Role { get; set; } = default!;
}
