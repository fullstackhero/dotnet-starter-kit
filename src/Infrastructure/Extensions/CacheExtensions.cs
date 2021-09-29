using DN.WebApi.Application.Abstractions.Services.General;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class CacheExtensions
    {
        public static void Set(this ICacheService cache, string key, byte[] value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            cache.Set(key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this ICacheService cache, string key, byte[] value, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return cache.SetAsync(key, value, new DistributedCacheEntryOptions(), token);
        }

        public static void SetString(this ICacheService cache, string key, string value)
        {
            cache.SetString(key, value, new DistributedCacheEntryOptions());
        }

        public static void SetString(this ICacheService cache, string key, string value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            cache.Set(key, Encoding.UTF8.GetBytes(value), options);
        }

        public static Task SetStringAsync(this ICacheService cache, string key, string value, CancellationToken token = default(CancellationToken))
        {
            return cache.SetStringAsync(key, value, new DistributedCacheEntryOptions(), token);
        }

        public static Task SetStringAsync(this ICacheService cache, string key, string value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return cache.SetAsync(key, Encoding.UTF8.GetBytes(value), options, token);
        }

        public static string GetString(this ICacheService cache, string key)
        {
            byte[] data = cache.Get(key);
            if (data == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static async Task<string> GetStringAsync(this ICacheService cache, string key, CancellationToken token = default(CancellationToken))
        {
            byte[] data = await cache.GetAsync(key, token).ConfigureAwait(false);
            if (data == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(data, 0, data.Length);
        }
    }
}