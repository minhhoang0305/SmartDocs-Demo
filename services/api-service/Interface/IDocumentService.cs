using Microsoft.AspNetCore.Http.Metadata;

namespace api_service.Interface;

public interface IDocumentService
{
    Task<object> UploadDocumentAsync(IFormFile file);
}