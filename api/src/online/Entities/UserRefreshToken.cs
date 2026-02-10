namespace Online.Entities;

public class UserRefreshToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
}
