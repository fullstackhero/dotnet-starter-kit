namespace DN.WebApi.Infrastructure.Caching;

public class CacheSettings
{
    public bool PreferRedis { get; set; }
    public string? RedisURL { get; set; }
}