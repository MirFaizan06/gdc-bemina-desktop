using CollegeAdmin.Application.Caching;

namespace CollegeAdmin.Desktop.Tests;

/// <summary>Final Convergence Phase P1-17: always calls through to `fetch` — no caching, no
/// TTL — so existing ViewModel tests that assert on API call counts/results are unaffected by
/// ILookupCache's presence. LookupCache itself (the real implementation) has its own dedicated tests.</summary>
internal sealed class FakeLookupCache : ILookupCache
{
    public Task<IReadOnlyList<T>> GetOrFetchAsync<T>(string key, Func<CancellationToken, Task<IReadOnlyList<T>>> fetch, CancellationToken cancellationToken = default) =>
        fetch(cancellationToken);

    public void Invalidate(string key)
    {
    }

    public void InvalidateAll()
    {
    }
}
