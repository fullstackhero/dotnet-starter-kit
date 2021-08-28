using DN.WebApi.Domain.Contracts;
using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Application.Constants
{
    public static class CacheKeys
    {
        public static string GetDtoCacheKey<T>(object id) where T : BaseEntity
        {
            return $"{typeof(T).Name}-{id}";
        }
    }
}