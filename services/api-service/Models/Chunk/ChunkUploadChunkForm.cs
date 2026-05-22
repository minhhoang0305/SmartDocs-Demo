namespace api_service.Models;

public class ChunkUploadChunkForm
{
    public string UploadId { get; set; } = default!;

    public int ChunkIndex { get; set; }

    public int TotalChunks { get; set; }

    public IFormFile File { get; set; } = default!;
}

