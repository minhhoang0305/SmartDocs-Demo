namespace api_service.Models;

public class ChunkUploadCompleteForm
{
    public string UploadId { get; set; } = default!;

    public int TotalChunks { get; set; }

    public string FileName { get; set; } = default!;
}

