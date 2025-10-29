namespace Online.Common.Config;

public class JwtOptions
{
    public const string JwtOptionsKey = "JwtOptions";
    public required string Secret { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }

    // Token valid for 15 minutes by default
    public int ExpirationTimeInMinutes { get; set; } = 15;

    // Refresh Token valid for 7 days by default
    public int RefreshTokenExpirationTimeInMinutes { get; set; } = 10080;
}