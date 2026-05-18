using api_service.Services;
using api_service.Models;
using Microsoft.AspNetCore.Mvc;

namespace api_service.Controllers;

[ApiController]
[Route("api/chunk")]
public class ChunkUploadController : ControllerBase
{
    private readonly ChunkUploadService _chunkService;
    private readonly MinioService _minioService;

    public ChunkUploadController(
        ChunkUploadService chunkService,
        MinioService minioService)
    {
        _chunkService = chunkService;
        _minioService = minioService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadChunk(
        [FromForm] ChunkUploadChunkForm request)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { message = "File is required" });

        if (request.ChunkIndex < 0)
            return BadRequest(new { message = "chunkIndex must be >= 0" });

        if (request.TotalChunks <= 0)
            return BadRequest(new { message = "totalChunks must be > 0" });

        await _chunkService.SaveChunkAsync(
            request.UploadId,
            request.ChunkIndex,
            request.File);

        return Ok(new
        {
            message = $"Chunk {request.ChunkIndex} uploaded"
        });
    }

    [HttpPost("complete")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CompleteUpload(
        [FromForm] ChunkUploadCompleteForm request)
    {
        if (request.TotalChunks <= 0)
            return BadRequest(new { message = "totalChunks must be > 0" });

        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(new { message = "fileName is required" });

        var mergedFile =
            await _chunkService.MergeChunksAsync(
                request.UploadId,
                request.TotalChunks,
                request.FileName);

        var objectName =
            await _minioService
                .UploadLargeFileAsync(
                    mergedFile);

        _chunkService.Cleanup(request.UploadId);

        return Ok(new
        {
            message = "Upload completed",
            objectName
        });
    }
}
