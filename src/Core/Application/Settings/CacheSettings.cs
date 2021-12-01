namespace DN.WebApi.Application.Settings;

public class CacheSettings
{
    public bool PreferRedis { get; set; }
    public string? RedisURL { get; set; }
}