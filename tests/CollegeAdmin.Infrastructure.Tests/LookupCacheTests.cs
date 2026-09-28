using CollegeAdmin.Infrastructure.Caching;

namespace CollegeAdmin.Infrastructure.Tests;

/// <summary>Final Convergence Phase P1-17 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): "confirms
/// cache is never treated as authoritative for writes" — GetOrFetchAsync_ReturnsCachedValue_
/// WithoutCallingFetchAgain proves reads are actually cached, and the two Invalidate tests prove a
/// write path can always force the next read to hit the server again, which is what makes that
/// caching safe rather than a source of stale-data bugs.</summary>
public class LookupCacheTests
{
    [Fact]
    public async Task GetOrFetchAsync_FirstCall_InvokesFetch()
    {
        var cache = new LookupCache();
        var callCount = 0;

        var result = await cache.GetOrFetchAsync("programmes", _ =>
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<string>>(["A", "B"]);
        });

        Assert.Equal(1, callCount);
        Assert.Equal(["A", "B"], result);
    }

    [Fact]
    public async Task GetOrFetchAsync_SecondCall_ReturnsCachedValue_WithoutCallingFetchAgain()
    {
        var cache = new LookupCache();
        var callCount = 0;
        Task<IReadOnlyList<string>> Fetch(CancellationToken _)
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<string>>(["A"]);
        }

        await cache.GetOrFetchAsync("programmes", Fetch);
        await cache.GetOrFetchAsync("programmes", Fetch);

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrFetchAsync_DifferentKeys_AreCachedIndependently()
    {
        var cache = new LookupCache();

        var programmes = await cache.GetOrFetchAsync("programmes", _ => Task.FromResult<IReadOnlyList<string>>(["P"]));
        var subjects = await cache.GetOrFetchAsync("subjects", _ => Task.FromResult<IReadOnlyList<string>>(["S"]));

        Assert.Equal(["P"], programmes);
        Assert.Equal(["S"], subjects);
    }

    [Fact]
    public async Task Invalidate_ForcesTheNextCall_ToFetchAgain()
    {
        var cache = new LookupCache();
        var callCount = 0;
        Task<IReadOnlyList<string>> Fetch(CancellationToken _)
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<string>>(["A"]);
        }

        await cache.GetOrFetchAsync("programmes", Fetch);
        cache.Invalidate("programmes");
        await cache.GetOrFetchAsync("programmes", Fetch);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Invalidate_OnlyAffectsTheNamedKey()
    {
        var cache = new LookupCache();
        var subjectsCallCount = 0;
        await cache.GetOrFetchAsync("programmes", _ => Task.FromResult<IReadOnlyList<string>>(["P"]));
        await cache.GetOrFetchAsync("subjects", _ =>
        {
            subjectsCallCount++;
            return Task.FromResult<IReadOnlyList<string>>(["S"]);
        });

        cache.Invalidate("programmes");
        await cache.GetOrFetchAsync("subjects", _ =>
        {
            subjectsCallCount++;
            return Task.FromResult<IReadOnlyList<string>>(["S"]);
        });

        Assert.Equal(1, subjectsCallCount);
    }

    [Fact]
    public async Task GetOrFetchAsync_ExpiredEntry_FetchesAgain()
    {
        var cache = new LookupCache(TimeSpan.FromMilliseconds(1));
        var callCount = 0;
        Task<IReadOnlyList<string>> Fetch(CancellationToken _)
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<string>>(["A"]);
        }

        await cache.GetOrFetchAsync("programmes", Fetch);
        await Task.Delay(20);
        await cache.GetOrFetchAsync("programmes", Fetch);

        Assert.Equal(2, callCount);
    }
}
