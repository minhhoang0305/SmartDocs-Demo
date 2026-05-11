using Grpc.Core;

namespace processing_service.gRPC;
public class DocumentGrpcService : DocumentGrpc.DocumentGrpcBase
{
    public override Task<DocumentReply> GetDocumentAnalysis(DocumentRequest request, ServerCallContext context)
    {
        return Task.FromResult(new DocumentReply
        {
            Result = "Kết quả của document: {request.DocumentId}"
        });
    }
}
