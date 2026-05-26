using Amazon.S3;
using Amazon.S3.Transfer;

namespace Online.Common.Utilities;

public class ImageUploader(ITransferUtility transferUtility, IConfiguration configuration)
{
    private readonly ITransferUtility _transferUtility = transferUtility;
    private readonly string _bucketName = configuration["AWSBucketName"]
            ?? throw new ArgumentNullException(nameof(configuration), "Bucket name is missing from configuration.");

    public async Task UploadFileAsync(Stream fileStream, string keyName, CancellationToken ct = default)
    {
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = keyName,
            BucketName = _bucketName,
            CannedACL = S3CannedACL.PublicRead
        };
        await _transferUtility.UploadAsync(uploadRequest, ct);
    }
}
