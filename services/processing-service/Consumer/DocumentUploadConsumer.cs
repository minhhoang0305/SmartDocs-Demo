using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using processing_service.Services;
using shared.Event;

namespace processing_service.Consumer;
public class DocumentUploadConsumer : BackgroundService
{
    private const string ExchangeName = "request.exchange";
    private const string QueueName = "document-uploaded";
    private const string RoutingKey = "document-uploaded";
    private const string TraceIdHeader = "traceId";
    private const string LegacyTraceIdHeader = "trace-Id";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnection _connection;
    private readonly ILogger<DocumentUploadConsumer> _logger;
    private IModel? _channel;

    public DocumentUploadConsumer(
        IServiceScopeFactory scopeFactory,
        IConnection connection,
        ILogger<DocumentUploadConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = connection;
        _logger = logger;
    }
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _channel = _connection.CreateModel();

        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 5,
            global: false);

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object> {{"x-queue-type", "quorum"}});
        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct);

        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, eventArgs) =>
        {
            await HandleMessageAsync(eventArgs, cancellationToken);
        };
        
        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);
        

        var queueInfo = _channel.QueueDeclarePassive(QueueName);

        _logger.LogInformation(
            "RabbitMQ consumer started queue={Queue} exchange={Exchange} routingKey={RoutingKey} readyMessages={MessageCount} consumers={ConsumerCount}",
            QueueName,
            ExchangeName,
            RoutingKey,
            queueInfo.MessageCount,
            queueInfo.ConsumerCount);

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            _logger.LogError("RabbitMQ channel is not initialized");
            return;
        }

        var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        var traceId =
            GetHeaderValue(eventArgs.BasicProperties.Headers, TraceIdHeader)
            ?? GetHeaderValue(eventArgs.BasicProperties.Headers, LegacyTraceIdHeader)
            ?? string.Empty;

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId
        });

        try
        {
            _logger.LogInformation(
                "Received RabbitMQ message deliveryTag={DeliveryTag} routingKey={RoutingKey} body={Body} traceId = {TraceId}",
                eventArgs.DeliveryTag,
                eventArgs.RoutingKey,
                json,
                traceId);

            var message = JsonSerializer.Deserialize<DocumentUploadEvent>(json);
            if (message is null)
            {
                _logger.LogWarning(
                    "Ignoring empty RabbitMQ message deliveryTag={DeliveryTag} traceId = {TraceId}",
                    eventArgs.DeliveryTag,
                    traceId);

                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation(
                "Processing document upload event documentId={DocumentId} fileName={FileName} traceId = {TraceId}",
                message.DocumentID,
                message.Filename,
                traceId);

            using var scope = _scopeFactory.CreateScope();
            var redis = scope.ServiceProvider.GetRequiredService<RedisService>();
            var result = $"Document {message.Filename} đăng thành công";

            await redis.SetCacheAsync(
                $"document:{message.DocumentID}",
                result);

            _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);

            _logger.LogInformation(
                "Processed document upload event documentId={DocumentId} fileName={FileName} traceId = {TraceId}",
                message.DocumentID,
                message.Filename,
                traceId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Invalid RabbitMQ message payload deliveryTag={DeliveryTag} body={Body} traceId = {TraceId}",
                eventArgs.DeliveryTag,
                json,
                traceId);

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "RabbitMQ message processing canceled deliveryTag={DeliveryTag} traceId = {TraceId}",
                eventArgs.DeliveryTag,
                traceId);

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process RabbitMQ message deliveryTag={DeliveryTag} routingKey={RoutingKey} traceId = {TraceId}",
                eventArgs.DeliveryTag,
                eventArgs.RoutingKey,
                traceId);

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true);
        }
    }

    private static string? GetHeaderValue(
        IDictionary<string, object>? headers,
        string headerName)
    {
        if (headers is null ||
            !headers.TryGetValue(headerName, out var value))
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value.ToString()
        };
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer queue={Queue}", QueueName);

        _channel?.Close();
        _channel?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}
