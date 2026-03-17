namespace Online.Common.Config;

public class AppConfig
{
    public required string EncryptionKey { get; set; }
    public required string EncryptionSalt { get; set; }
    public int PendingTimeoutMinutes { get; set; } = 10;
}
