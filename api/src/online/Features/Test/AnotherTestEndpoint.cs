using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Online.Common.Config;

namespace Online.Features.Test;

public class AnotherTestEndpoint(IAmazonS3 s3Client, IOptions<AWSConfig> awsConfig) : EndpointWithoutRequest<object>
{
    private readonly AWSConfig config = awsConfig.Value;
    public override void Configure()
    {
        Get("/test/another");
        Description(x => x.WithName("TestAnother"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        using var result = await s3Client.GetObjectAsync(config.BucketName, "Screenshot From 2026-03-24 21-59-25.png", ct);
        var fileInfo = new
        {
            FileName = result.Key,
            SizeInBytes = result.Headers.ContentLength,
            ContentType = result.Headers.ContentType,
            LastModified = result.LastModified,
            ETag = result.ETag
        };

        await Send.OkAsync(fileInfo, ct);
    }
}
