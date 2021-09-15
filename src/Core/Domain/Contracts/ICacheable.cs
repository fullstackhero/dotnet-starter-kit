using System;

namespace DN.WebApi.Domain.Contracts
{
    public interface ICacheableQuery
    {
        bool BypassCache { get; }
        string CacheKey { get; }
        TimeSpan? SlidingExpiration { get; }
    }
}