using api_service.Data;
using api_service.Interface;
using api_service.Models;
using shared.Event;

namespace api_service.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;
    private readonly IMessagePublisher _publisher;
    private readonly MinioService _minioService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        AppDbContext context,
        IMessagePublisher publisher,
        MinioService minioService,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _publisher = publisher;
        _minioService = minioService;
        _logger = logger;
    }

    public async Task<object> UploadDocumentAsync(IFormFile file)
    {
        _logger.LogInformation(
            "Uploading document fileName={FileName} contentType={ContentType} size={FileSize}",
            file.FileName,
            file.ContentType,
            file.Length);

        var fileUrl = await _minioService.UploadFileAsync(file);
        var document = new Documents()
        {
            FileName = file.FileName,
            FileUrl = fileUrl,
            CreateAt = DateTime.UtcNow 
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Saved document documentId={DocumentId} fileName={FileName} fileUrl={FileUrl}",
            document.Id,
            document.FileName,
            document.FileUrl);

        var uploadEvent = new DocumentUploadEvent
        {
            DocumentID = document.Id,
            Filename = document.FileName,
            Fileurl = document.FileUrl
        };

        _publisher.Publish("request.exchange", "document-uploaded", uploadEvent);

        _logger.LogInformation(
            "Queued document upload event documentId={DocumentId} routingKey={RoutingKey}",
            document.Id,
            "document-uploaded");

        return document;
    }
}
