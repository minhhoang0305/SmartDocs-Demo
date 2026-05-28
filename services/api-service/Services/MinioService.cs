using api_service.Options;
using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;

namespace api_service.Services;

public class MinioService
{
    private readonly MinioOptions _minioOptions;
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioService> _logger;

    public MinioService(
        IOptions<MinioOptions> minioOptions,
        ILogger<MinioService> logger)
    {
        _minioOptions = minioOptions.Value;
        _logger = logger;

        var endPoint = _minioOptions.Endpoint;
        var accessKey = _minioOptions.AccessKey;
        var secretKey = _minioOptions.SecretKey;

        if (string.IsNullOrWhiteSpace(endPoint))
            throw new InvalidOperationException("Minio:Endpoint is required");
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new InvalidOperationException("Minio:AccessKey is required");
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Minio:SecretKey is required");

        _minioClient = new MinioClient()
            .WithEndpoint(endPoint)
            .WithCredentials(accessKey, secretKey)
            .Build();

        _logger.LogInformation("Configured MinIO client endpoint={Endpoint}", endPoint);
    }

    private string GetBucketName()
    {
        var bucketName = _minioOptions.BucketName;
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Minio:BucketName is required");
        return bucketName;
    }

    private async Task EnsureBucketExistsAsync(string bucketName)
    {
        try
        {
            var exists =
                await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(bucketName));

            if (exists)
            {
                _logger.LogInformation("MinIO bucket exists bucket={BucketName}", bucketName);
                return;
            }

            _logger.LogInformation("Creating MinIO bucket bucket={BucketName}", bucketName);

            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName));

            _logger.LogInformation("Created MinIO bucket bucket={BucketName}", bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to ensure MinIO bucket exists bucket={BucketName}",
                bucketName);

            throw;
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        var bucketName = GetBucketName();
        var objectName = $"{Guid.NewGuid()}-{file.FileName}";

        try
        {
            _logger.LogInformation(
                "Uploading file to MinIO bucket={BucketName} objectName={ObjectName} fileName={FileName} contentType={ContentType} fileSize={FileSize}",
                bucketName,
                objectName,
                file.FileName,
                file.ContentType,
                file.Length);

            await EnsureBucketExistsAsync(bucketName);

            using var stream = file.OpenReadStream();

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            var objectPath = $"{bucketName}/{objectName}";

            _logger.LogInformation(
                "Uploaded file to MinIO bucket={BucketName} objectName={ObjectName} objectPath={ObjectPath}",
                bucketName,
                objectName,
                objectPath);

            return objectPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload file to MinIO bucket={BucketName} objectName={ObjectName} fileName={FileName}",
                bucketName,
                objectName,
                file.FileName);

            throw;
        }
    }

    public async Task<string> UploadLargeFileAsync(string filePath)
    {
        var bucketName = GetBucketName();
        var fileName = Path.GetFileName(filePath);

        var objectName =
            $"documents/{Guid.NewGuid()}-{fileName}";

        try
        {
            _logger.LogInformation(
                "Uploading large file to MinIO bucket={BucketName} objectName={ObjectName} filePath={FilePath}",
                bucketName,
                objectName,
                filePath);

            await EnsureBucketExistsAsync(bucketName);

            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read);

            var putObjectArgs =
                new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("application/octet-stream");

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation(
                "Uploaded large file to MinIO bucket={BucketName} objectName={ObjectName} fileSize={FileSize}",
                bucketName,
                objectName,
                stream.Length);

            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload large file to MinIO bucket={BucketName} objectName={ObjectName} filePath={FilePath}",
                bucketName,
                objectName,
                filePath);

            throw;
        }
    }
}
