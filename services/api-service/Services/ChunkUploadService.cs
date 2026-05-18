namespace api_service.Services;

public class ChunkUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _tempRootPath;

    public ChunkUploadService(
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _environment = environment;

        var configuredTempRoot =
            configuration["ChunkUpload:TempRootPath"];

        _tempRootPath =
            string.IsNullOrWhiteSpace(configuredTempRoot)
                ? Path.Combine(
                    Path.GetTempPath(),
                    "smartdocs",
                    "temp")
                : configuredTempRoot;
    }

    private static string SanitizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", nameof(value));

        var trimmed = value.Trim();

        if (trimmed == "." || trimmed == "..")
            throw new ArgumentException("Invalid value.", nameof(value));

        trimmed = trimmed
            .Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            trimmed = trimmed.Replace(invalid, '_');
        }

        return trimmed;
    }

    public async Task SaveChunkAsync(
        string uploadId,
        int chunkIndex,
        IFormFile chunk)
    {
        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));

        var safeUploadId = SanitizePathSegment(uploadId);

        var tempPath =
            Path.Combine(
                _tempRootPath,
                safeUploadId);

        Directory.CreateDirectory(tempPath);

        var chunkPath =
            Path.Combine(
                tempPath,
                $"chunk-{chunkIndex}");

        await using var stream =
            new FileStream(
                chunkPath,
                FileMode.Create);

        await chunk.CopyToAsync(stream);
    }

    public async Task<string> MergeChunksAsync(
        string uploadId,
        int totalChunks,
        string fileName)
    {
        if (totalChunks <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalChunks));

        var safeUploadId = SanitizePathSegment(uploadId);

        var tempPath =
            Path.Combine(
                _tempRootPath,
                safeUploadId);

        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            throw new ArgumentException("fileName is required.", nameof(fileName));

        var mergedFilePath =
            Path.Combine(
                tempPath,
                safeFileName);

        await using var output =
            new FileStream(
                mergedFilePath,
                FileMode.Create);

        for (int i = 0; i < totalChunks; i++)
        {
            var chunkPath =
                Path.Combine(
                    tempPath,
                    $"chunk-{i}");

            await using var input =
                new FileStream(
                    chunkPath,
                    FileMode.Open);

            await input.CopyToAsync(output);
        }

        return mergedFilePath;
    }

    public void Cleanup(string uploadId)
    {
        var safeUploadId = SanitizePathSegment(uploadId);

        var tempPath =
            Path.Combine(
                _tempRootPath,
                safeUploadId);

        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, true);
        }
    }
}
