using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using processing_service.Services;
using shared.Event;

namespace processing_service.Consumer;
public class DocumentUploadConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentUploadConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "document-uploaded",
            durable:true,
            exclusive:false,
            autoDelete: false,
            arguments: null
        );
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ae) =>
        {
            var body = ae.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Tin nhắn nhận: {json}");
            var message = JsonSerializer.Deserialize<DocumentUploadEvent>(json);
            Console.WriteLine($"Processing {message?.Filename}");
            var scope = _scopeFactory.CreateScope();
            var redis = scope.ServiceProvider.GetRequiredService<RedisService>();
            var result = $"Document {message?.Filename} đăng thành công";
            await redis.SetCacheAsync($"document:{message?.DocumentID}", result);
            Console.WriteLine($"Đã lưu Redis");
        };
        
        channel.BasicConsume(queue:"document-uploaded", autoAck:true, consumer : consumer);
        return Task.CompletedTask;
    }
}