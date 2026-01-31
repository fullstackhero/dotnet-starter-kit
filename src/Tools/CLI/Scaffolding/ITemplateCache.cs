namespace FSH.CLI.Scaffolding;

/// <summary>
/// Caching layer for templates
/// </summary>
internal interface ITemplateCache
{
    /// <summary>
    /// Gets a cached template by key
    /// </summary>
    string? GetTemplate(string key);

    /// <summary>
    /// Stores a template in the cache
    /// </summary>
    void SetTemplate(string key, string template);

    /// <summary>
    /// Checks if a template is cached
    /// </summary>
    bool ContainsTemplate(string key);

    /// <summary>
    /// Removes a template from the cache
    /// </summary>
    void RemoveTemplate(string key);

    /// <summary>
    /// Clears all cached templates
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache performance statistics
/// </summary>
internal record CacheStatistics(int TotalEntries, int HitCount, int MissCount, double HitRatio);