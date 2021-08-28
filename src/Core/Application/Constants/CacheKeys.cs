namespace DN.WebApi.Application.Constants
{
    public static class CacheKeys
    {
        public static string GetEntityCacheKey<T>(object id) where T : class
        {
            return $"{typeof(T).Name}-{id}";
        }
    }
}