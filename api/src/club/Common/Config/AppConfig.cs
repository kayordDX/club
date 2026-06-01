namespace Online.Common.Config;

public class AppConfig
{
    public required string EncryptionKey { get; set; }
    public required string EncryptionSalt { get; set; }
    public string DockerSubnet { get; set; } = "172.18.0.0/16";
    public int PendingTimeoutMinutes { get; set; } = 10;
}
