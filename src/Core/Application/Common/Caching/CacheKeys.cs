namespace FSH.WebApi.Application.Common.Caching;

public static class CacheKeys
{
    public static string GetCacheKey<T>(object id)
    where T : IEntity
    {
        return $"{typeof(T).Name}-{id}";
    }

    public static string GetCacheKey(string name, object id)
    {
        return $"{name}-{id}";
    }
}