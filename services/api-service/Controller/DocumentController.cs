using api_service.Data;
using api_service.Models;
using api_service.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using shared.Event;


namespace api_service.Controller
{
    [ApiController]
    [Route("api/document")]
    public class DocumentController : ControllerBase
    {
        private readonly MinioService _minioService;
        private readonly RabbitmqPublish _publisher;
        private readonly AppDbContext _context;
        public DocumentController (MinioService minioService, AppDbContext context, RabbitmqPublish publisher)
        {
            _minioService = minioService;
            _context = context;
            _publisher = publisher;
        }
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if(file == null || file.Length == 0)
            {
                return BadRequest("File bắt buộc");
            }
            var fileUrl = await _minioService.UploadFileAsync(file);
            var document = new Documents()
            {
              FileName = file.FileName,
              FileUrl = fileUrl,
              CreateAt = DateTime.UtcNow 
            };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var uploadEvent = new DocumentUploadEvent
            {
                DocumentID = document.Id,
                Filename =document.FileName,
                Fileurl = document.FileUrl
            };
            var jsonMessage = JsonSerializer.Serialize(uploadEvent);
            await _publisher.Publish(jsonMessage);

            return Ok(new
            {
                message = "Đăng thành công",
                fileUrl
            });
        }
    }
}