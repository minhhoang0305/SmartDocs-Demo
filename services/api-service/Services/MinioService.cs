using Minio;
using Minio.DataModel.Args;

namespace api_service.Services;

public class MinioService
{
    private readonly IConfiguration _configuration;
    private readonly IMinioClient _minioClient;

    public MinioService(IConfiguration configuration)
    {
        _configuration = configuration;

        var endPoint = _configuration["Minio:Endpoint"];
        var accessKey = _configuration["Minio:AccessKey"];
        var secretKey = _configuration["Minio:SecretKey"];

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
    }

    private string GetBucketName()
    {
        var bucketName = _configuration["Minio:BucketName"];
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Minio:BucketName is required");
        return bucketName;
    }

    private async Task EnsureBucketExistsAsync(string bucketName)
    {
        var exists =
            await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucketName));

        if (!exists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName));
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        var bucketName = GetBucketName();
        var objectName = $"{Guid.NewGuid()}-{file.FileName}";

        await EnsureBucketExistsAsync(bucketName);

        using var stream = file.OpenReadStream();

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(file.ContentType);

        await _minioClient.PutObjectAsync(putObjectArgs);
        return $"{bucketName}/{objectName}";
    }

    public async Task<string> UploadLargeFileAsync(string filePath)
    {
        var bucketName = GetBucketName();

        await EnsureBucketExistsAsync(bucketName);

        var fileName = Path.GetFileName(filePath);

        var objectName =
            $"documents/{Guid.NewGuid()}-{fileName}";

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

        return objectName;
    }
}
