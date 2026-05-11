using RabbitMQ.Client;
using StackExchange.Redis;

namespace processing_service.Services;

public class RedisService
{
    private readonly IDatabase _database;
    public RedisService(IConfiguration configuration)
    {
        var redis =ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"]!);
        _database = redis.GetDatabase();
    }
    public async Task SetCacheAsync(
        string key,
        string value
    )
    {
        await _database.StringSetAsync(
            key,
            value,
            TimeSpan.FromMinutes(30)
        );
    }
    public async Task<string?> GetCacheAsync(string key)
    {
        return await _database.StringGetAsync(key);
    }
}