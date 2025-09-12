using Microsoft.AspNetCore.Identity;

namespace Online.Entities;

public class User : IdentityUser<int>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }

    public override string ToString()
    {
        return FirstName + " " + LastName;
    }
}