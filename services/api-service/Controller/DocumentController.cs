using api_service.Data;
using api_service.Models;
using api_service.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using shared.Event;
using Microsoft.AspNetCore.Authorization;
using api_service.Interface;


namespace api_service.Controller
{
    [ApiController]
    [Route("api/document")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        public DocumentController (IDocumentService documentService)
        {
            _documentService = documentService;
        }
        [HttpPost("upload")]
        [Authorize (Policy = "AdminOnly")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if(file == null || file.Length == 0)
            {
                return BadRequest("File bắt buộc");
            }
            var result = await _documentService.UploadDocumentAsync(file);
            return Ok(result);
              
        }
        [HttpGet("all")]
        [Authorize (Policy = "UserOnly")]
        public async Task<IActionResult> GetAllDocuments()
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }
    }

}