using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Club.Common.Config;
using Club.Common.Utilities;

namespace Club.Common.Extensions;

public static class AWSExtensions
{
    public static IServiceCollection ConfigureAWS(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("AWS");
        services.Configure<AWSConfig>(section);
        var awsConfig = section.Get<AWSConfig>() ?? throw new ArgumentNullException(nameof(configuration), "AWS configuration is missing.");

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var credentials = new BasicAWSCredentials(awsConfig.AccessKeyId, awsConfig.SecretAccessKey);
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsConfig.Region)
            };
            return new AmazonS3Client(credentials, config);
        });

        services.AddSingleton<ITransferUtility>(sp =>
        {
            var s3Client = sp.GetRequiredService<IAmazonS3>();
            return new TransferUtility(s3Client);
        });
        services.AddSingleton<ImageUploader>();
        return services;
    }
}
