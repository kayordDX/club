using Microsoft.AspNetCore.Identity;

namespace Club.Entities;

public class UserRole : IdentityUserRole<Guid>
{
    public int Id { get; set; }
    public int? FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public Role Role { get; set; } = default!;
}
