namespace CollegeAdmin.Infrastructure.Api;

public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Final Convergence Phase P1-16 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): exponential backoff
/// + jitter, retrying only when the server/transport itself marked the failure retryable
/// (<see cref="ApiRequestException.Retryable"/> — network error, timeout, empty response; never a
/// validation/auth/conflict/not-found failure, which the server or ApiEnvelopeHttp already code as
/// non-retryable). The delay function is injectable so tests can run deterministically and instantly
/// instead of actually sleeping.
/// </summary>
public sealed class RetryPolicy(int maxAttempts = 3, Func<TimeSpan, CancellationToken, Task>? delay = null, Random? random = null) : IRetryPolicy
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;
    private readonly Random _random = random ?? Random.Shared;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (ApiRequestException ex) when (ex.Retryable && attempt < maxAttempts)
            {
                var backoffMs = 200 * Math.Pow(2, attempt - 1);
                var jitterMs = _random.Next(0, 100);
                await _delay(TimeSpan.FromMilliseconds(backoffMs + jitterMs), cancellationToken);
            }
        }
    }
}
