using System.Diagnostics;
using System.Text;
using System.Text.Json;
using api_service.Interface;
using RabbitMQ.Client;

namespace api_service.Services;

public class RabbitmqPublish: IMessagePublisher
{
    private const string TraceIdHeader = "traceId";

    private readonly IConnection _connection;
    private readonly ILogger<RabbitmqPublish> _logger;

    public RabbitmqPublish(
        IConnection connection,
        ILogger<RabbitmqPublish> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public void Publish<T>(string exchange, string routingKey, T message)
    {
        using var channel = _connection.CreateModel();
        channel.QueueDeclare(
            queue: routingKey,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object> {{"x-queue-type", "quorum"}});
        
        channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        channel.QueueBind(
            queue: routingKey,
            exchange: exchange,
            routingKey: routingKey);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;
        properties.Headers = new Dictionary<string, object>
        {
            { TraceIdHeader, traceId }
        };

        channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: properties,
            body: body
        );

        _logger.LogInformation(
            "Published {MessageType} to RabbitMQ exchange={Exchange} routingKey={RoutingKey}",
            typeof(T).Name,
            exchange,
            routingKey);
    }
}
