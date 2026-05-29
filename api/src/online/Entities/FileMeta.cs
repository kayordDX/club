namespace Online.Entities;

public class FileMeta : AuditableEntity
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string S3Key { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public string? ContentType { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsProcessingError { get; set; }
    public string? ProcessingMessage { get; set; }
}
