namespace Online.Features.Account.Refresh.List;

public class RefreshListResponse
{
    public Guid Id { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}
