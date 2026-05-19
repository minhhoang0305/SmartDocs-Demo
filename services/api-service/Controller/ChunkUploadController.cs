using api_service.Services;
using api_service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace api_service.Controllers;

[ApiController]
[Authorize]
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
        [FromForm] string uploadId,
        [FromForm] int chunkIndex,
        [FromForm] int totalChunks,
        IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File is required" });

        if (chunkIndex < 0)
            return BadRequest(new { message = "chunkIndex must be >= 0" });

        if (totalChunks <= 0)
            return BadRequest(new { message = "totalChunks must be > 0" });

        await _chunkService.SaveChunkAsync(
            uploadId,
            chunkIndex,
            file);

        return Ok(new
        {
            message = $"Chunk {chunkIndex} uploaded"
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
