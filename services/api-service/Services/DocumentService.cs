using System.Text.Json;
using api_service.Data;
using api_service.Interface;
using api_service.Models;
using shared.Event;

namespace api_service.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;
    private readonly RabbitmqPublish _publisher;
    private readonly MinioService _minioService;
    public DocumentService(AppDbContext context, RabbitmqPublish publisher, MinioService minioService)
    {
        _context = context;
        _publisher = publisher;
        _minioService = minioService;
    }
    public async Task<object> UploadDocumentAsync(IFormFile file)
    {
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
            Filename = document.FileName,
            Fileurl = document.FileUrl
        };
        var jsonMessage = JsonSerializer.Serialize(uploadEvent);
        await _publisher.Publish(jsonMessage);
        return document;
    }
}