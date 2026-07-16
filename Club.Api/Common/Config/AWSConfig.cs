namespace Club.Common.Config;

public class AWSConfig
{
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public required string Region { get; set; } = "af-south-1";
    public required string BucketName { get; set; }
}
