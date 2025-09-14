using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

public class BackblazeS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<BackblazeS3Service> _logger;
    private readonly string _bucketName;

    // 100 MB üstü dosyaları chunk'la
    private const long ChunkThreshold = 100 * 1024 * 1024;

    public BackblazeS3Service(IOptions<BackblazeB2Settings> options, ILogger<BackblazeS3Service> logger)
    {
        _logger = logger;
        var settings = options.Value;

        _bucketName = settings.BucketName;

        var config = new AmazonS3Config
        {
            ServiceURL = settings.ServiceURL,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(settings.KeyId, settings.AppKey, config);
    }

    public async Task<string> UploadFileAsync(string fileName, Stream fileStream, long fileSize)
    {
        if (fileSize > ChunkThreshold)
        {
            return await UploadLargeFileAsync(fileName, fileStream, fileSize);
        }
        else
        {
            return await UploadSmallFileAsync(fileName, fileStream, fileSize);
        }
    }

    private async Task<string> UploadSmallFileAsync(string fileName, Stream fileStream, long fileSize)
    {
        try
        {
            _logger.LogInformation($"[SMALL UPLOAD] Starting upload for {fileName} ({fileSize / 1024 / 1024} MB)");
            var transferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = _bucketName
            };

            await transferUtility.UploadAsync(uploadRequest);

            _logger.LogInformation($"[SMALL UPLOAD] Completed upload for {fileName}");
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading small file");
            throw;
        }
    }

    private async Task<string> UploadLargeFileAsync(string fileName, Stream fileStream, long fileSize)
    {
        try
        {

            _logger.LogInformation($"[CHUNK UPLOAD] Starting upload for {fileName} ({fileSize / 1024 / 1024} MB)");
            var transferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = _bucketName,
                PartSize = 50 * 1024 * 1024,
            };
            uploadRequest.UploadProgressEvent += (sender, e) =>
            {
                _logger.LogInformation($"[CHUNK UPLOAD] {fileName} Progress: {e.TransferredBytes / 1024 / 1024} MB / {e.TotalBytes / 1024 / 1024} MB");
            };

            await transferUtility.UploadAsync(uploadRequest);
            _logger.LogInformation($"[CHUNK UPLOAD] Completed upload for {fileName}");
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading large file in chunks");
            throw;
        }
    }
}
