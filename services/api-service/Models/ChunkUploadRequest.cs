namespace api_service.Models;

public class ChunkUploadRequest
{
    public string UploadId { get; set; } = default!;

    public int ChunkIndex { get; set; }

    public int TotalChunks { get; set; }
}