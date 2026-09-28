using CollegeAdmin.Application.Caching;

namespace CollegeAdmin.Infrastructure.Caching;

public sealed class LookupCache(TimeSpan? ttl = null) : ILookupCache
{
    private readonly TimeSpan _ttl = ttl ?? TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, (object Data, DateTime ExpiresAtUtc)> _entries = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IReadOnlyList<T>> GetOrFetchAsync<T>(string key, Func<CancellationToken, Task<IReadOnlyList<T>>> fetch, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_entries.TryGetValue(key, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
            {
                return (IReadOnlyList<T>)cached.Data;
            }
        }
        finally
        {
            _lock.Release();
        }

        // Fetched outside the lock so a slow network call never blocks unrelated cache keys.
        var data = await fetch(cancellationToken);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _entries[key] = (data, DateTime.UtcNow + _ttl);
        }
        finally
        {
            _lock.Release();
        }

        return data;
    }

    public void Invalidate(string key) => _entries.Remove(key);

    public void InvalidateAll() => _entries.Clear();
}
