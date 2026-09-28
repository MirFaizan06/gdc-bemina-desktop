namespace CollegeAdmin.Application.Caching;

/// <summary>
/// Final Convergence Phase P1-17 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): a small generic
/// keyed in-memory cache for rarely-changing lookup lists (Programmes today — see LookupCache's own
/// remarks for why Departments/Subjects/capability-bootstrap aren't wired in yet). Deliberately NOT
/// SQLite-backed/persisted across launches — see docs/claude/18_DEVLOG.md Entry 49 for why that's
/// out of this session's honest scope. The cache is never authoritative: every write path that can
/// change a cached lookup's underlying data must call Invalidate for that same key.
/// </summary>
public interface ILookupCache
{
    Task<IReadOnlyList<T>> GetOrFetchAsync<T>(string key, Func<CancellationToken, Task<IReadOnlyList<T>>> fetch, CancellationToken cancellationToken = default);

    void Invalidate(string key);

    void InvalidateAll();
}
