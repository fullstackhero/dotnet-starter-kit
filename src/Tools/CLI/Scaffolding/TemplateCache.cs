using System.Collections.Concurrent;

namespace FSH.CLI.Scaffolding;

/// <summary>
/// In-memory cache for templates
/// </summary>
internal sealed class TemplateCache : ITemplateCache
{
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private int _hitCount;
    private int _missCount;

    public string? GetTemplate(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_cache.TryGetValue(key, out var template))
        {
            Interlocked.Increment(ref _hitCount);
            return template;
        }

        Interlocked.Increment(ref _missCount);
        return null;
    }

    public void SetTemplate(string key, string template)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(template);

        _cache[key] = template;
    }

    public bool ContainsTemplate(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        return _cache.ContainsKey(key);
    }

    public void RemoveTemplate(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        _cache.TryRemove(key, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _hitCount, 0);
        Interlocked.Exchange(ref _missCount, 0);
    }

    public CacheStatistics GetStatistics()
    {
        var totalRequests = _hitCount + _missCount;
        var hitRatio = totalRequests > 0 ? (double)_hitCount / totalRequests : 0.0;

        return new CacheStatistics(_cache.Count, _hitCount, _missCount, hitRatio);
    }
}