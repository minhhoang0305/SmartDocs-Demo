namespace api_service.Interface
{
    public interface IMessagePublisher
    {
        void Publish<T>(string exchange, string routingKey, T message);
    }
}