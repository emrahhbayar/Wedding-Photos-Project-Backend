using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

public class BackblazeS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<BackblazeS3Service> _logger;

    public BackblazeS3Service(ILogger<BackblazeS3Service> logger)
    {
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = "https://s3.eu-central-003.backblazeb2.com",
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client("your-key-id", "your-app-key", config);
    }

    public async Task<string> UploadFileAsync(string bucketName, string fileName, Stream fileStream)
    {
        try
        {
            var transferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = bucketName
            };

            await transferUtility.UploadAsync(uploadRequest);

            _logger.LogInformation($"File uploaded successfully: {fileName}");
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            throw;
        }
    }
}
