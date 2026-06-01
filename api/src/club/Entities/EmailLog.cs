namespace Online.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
