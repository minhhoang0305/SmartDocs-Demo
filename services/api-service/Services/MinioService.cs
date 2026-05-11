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

            _minioClient = new MinioClient()
            .WithEndpoint(endPoint)
            .WithCredentials(accessKey,secretKey)
            .Build();
        }
        public async Task<String> UploadFileAsync(IFormFile file)
        {
            var bucketName = _configuration["Minio:BucketName"];
            var objectName = $"{Guid.NewGuid()} - {file.FileName}";

            using var stream = file.OpenReadStream();

            var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
            return $"{bucketName} - {objectName}";
        }
    }
