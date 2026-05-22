using api_service.Models;

namespace api_service.Interface;

public interface IDocumentService
{
    Task<object> UploadDocumentAsync(IFormFile file);
    Task<IEnumerable<DocumentsRequest>> GetAllDocumentsAsync();
}