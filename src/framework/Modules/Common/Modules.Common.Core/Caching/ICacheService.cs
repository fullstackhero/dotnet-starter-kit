namespace FSH.Modules.Common.Core.Caching;

public interface ICacheService
{
    T? GetItem<T>(string key);
    Task<T?> GetItemAsync<T>(string key, CancellationToken token = default);

    void RefreshItem(string key);
    Task RefreshItemAsync(string key, CancellationToken token = default);

    void RemoveItem(string key);
    Task RemoveItemAsync(string key, CancellationToken token = default);

    void SetItem<T>(string key, T value, TimeSpan? slidingExpiration = null);
    Task SetItemAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default);
}