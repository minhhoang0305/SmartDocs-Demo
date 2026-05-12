using System.Text;
using RabbitMQ.Client;

namespace api_service.Services;
    public class RabbitmqPublish
    {
        private readonly IConfiguration _configuration;

        public RabbitmqPublish(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task Publish (string message)
    {
        var factory = new ConnectionFactory()
        {
            // HostName = _configuration["Rabbitmq:Host"]
            HostName = "rabbitmq"
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(
            queue: "document-uploaded",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null );
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: "document-uploaded",
            body: body
        );
        Console.WriteLine("Publish thành công");
    }
    }

