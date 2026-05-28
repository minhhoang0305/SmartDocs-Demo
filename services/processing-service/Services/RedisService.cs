using StackExchange.Redis;

namespace processing_service.Services;

public class RedisService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;

    public RedisService(
        IConfiguration configuration,
        ILogger<RedisService> logger)
    {
        _logger = logger;

        var connectionString = configuration["Redis:ConnectionString"]!;

        _logger.LogInformation("Connecting to Redis");

        var redis = ConnectionMultiplexer.Connect(connectionString);
        _database = redis.GetDatabase();

        _logger.LogInformation("Connected to Redis");
    }

    public async Task SetCacheAsync(
        string key,
        string value
    )
    {
        try
        {
            var expiry = TimeSpan.FromMinutes(30);

            await _database.StringSetAsync(
                key,
                value,
                expiry
            );

            _logger.LogInformation(
                "Set Redis cache key={CacheKey} expiryMinutes={ExpiryMinutes}",
                key,
                expiry.TotalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to set Redis cache key={CacheKey}",
                key);

            throw;
        }
    }

    public async Task<string?> GetCacheAsync(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);

            _logger.LogInformation(
                "Read Redis cache key={CacheKey} hit={CacheHit}",
                key,
                value.HasValue);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to read Redis cache key={CacheKey}",
                key);

            throw;
        }
    }
}
