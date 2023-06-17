namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Caching;

public class CacheSettings
{
    public bool UseDistributedCache { get; set; }
    public bool PreferRedis { get; set; }
    public string? RedisURL { get; set; }
}