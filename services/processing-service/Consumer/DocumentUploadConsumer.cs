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

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct);

        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey);

        var consumer = new EventingBasicConsumer(_channel);
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

        try
        {
            _logger.LogInformation(
                "Received RabbitMQ message deliveryTag={DeliveryTag} routingKey={RoutingKey} body={Body}",
                eventArgs.DeliveryTag,
                eventArgs.RoutingKey,
                json);

            var message = JsonSerializer.Deserialize<DocumentUploadEvent>(json);
            if (message is null)
            {
                _logger.LogWarning(
                    "Ignoring empty RabbitMQ message deliveryTag={DeliveryTag}",
                    eventArgs.DeliveryTag);

                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            _logger.LogInformation(
                "Processing document upload event documentId={DocumentId} fileName={FileName}",
                message.DocumentID,
                message.Filename);

            using var scope = _scopeFactory.CreateScope();
            var redis = scope.ServiceProvider.GetRequiredService<RedisService>();
            var result = $"Document {message.Filename} đăng thành công";

            await redis.SetCacheAsync(
                $"document:{message.DocumentID}",
                result);

            _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);

            _logger.LogInformation(
                "Processed document upload event documentId={DocumentId} fileName={FileName}",
                message.DocumentID,
                message.Filename);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Invalid RabbitMQ message payload deliveryTag={DeliveryTag} body={Body}",
                eventArgs.DeliveryTag,
                json);

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "RabbitMQ message processing canceled deliveryTag={DeliveryTag}",
                eventArgs.DeliveryTag);

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process RabbitMQ message deliveryTag={DeliveryTag} routingKey={RoutingKey}",
                eventArgs.DeliveryTag,
                eventArgs.RoutingKey);

            _channel.BasicNack(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer queue={Queue}", QueueName);

        _channel?.Close();
        _channel?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}
