namespace processing_service.Options;
public class RedisOption
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set;} = string.Empty;
}