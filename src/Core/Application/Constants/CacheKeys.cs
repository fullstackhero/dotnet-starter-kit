namespace DN.WebApi.Application.Constants
{
    public static class CacheKeys
    {
        public static string GetEntityCacheKey<T>(Guid id) where T : class
        {
            return $"{typeof(T).FullName}-{id}";
        }
    }
}