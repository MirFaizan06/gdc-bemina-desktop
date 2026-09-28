using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Infrastructure.Tests;

/// <summary>Final Convergence Phase P1-16 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): deterministic,
/// instant tests — the injected delay function records requested durations instead of actually
/// sleeping, and a fixed-seed Random keeps jitter reproducible.</summary>
public class RetryPolicyTests
{
    private static ApiRequestException Retryable() =>
        new("NETWORK_ERROR", "Could not reach the server.", retryable: true, new InvalidOperationException());

    private static ApiRequestException NotRetryable() =>
        new(new ApiErrorPayload { Code = "VALIDATION_ERROR", Message = "Bad input.", Retryable = false });

    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt_NeverDelays()
    {
        var delays = new List<TimeSpan>();
        var policy = new RetryPolicy(delay: (d, _) => { delays.Add(d); return Task.CompletedTask; });

        var result = await policy.ExecuteAsync(() => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Empty(delays);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesRetryableFailures_WithExponentialBackoffAndJitter()
    {
        var delays = new List<TimeSpan>();
        var attempt = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delay: (d, _) => { delays.Add(d); return Task.CompletedTask; }, random: new Random(1));

        var result = await policy.ExecuteAsync(() =>
        {
            attempt++;
            if (attempt < 3)
            {
                throw Retryable();
            }
            return Task.FromResult("ok");
        });

        Assert.Equal("ok", result);
        Assert.Equal(3, attempt);
        Assert.Equal(2, delays.Count);
        // Exponential: attempt 1's backoff (200ms base) < attempt 2's (400ms base), jitter [0,100) on each.
        Assert.InRange(delays[0].TotalMilliseconds, 200, 300);
        Assert.InRange(delays[1].TotalMilliseconds, 400, 500);
    }

    [Fact]
    public async Task ExecuteAsync_StopsAfterMaxAttempts_AndThrowsTheLastFailure()
    {
        var attempt = 0;
        var policy = new RetryPolicy(maxAttempts: 3, delay: (_, _) => Task.CompletedTask);

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => policy.ExecuteAsync<object>(() =>
        {
            attempt++;
            throw Retryable();
        }));

        Assert.Equal(3, attempt); // 1 initial + 2 retries, then gives up
        Assert.Equal("NETWORK_ERROR", ex.Code);
    }

    [Fact]
    public async Task ExecuteAsync_NeverRetries_WhenTheFailureIsNotRetryable()
    {
        var attempt = 0;
        var delayCalled = false;
        var policy = new RetryPolicy(delay: (_, _) => { delayCalled = true; return Task.CompletedTask; });

        await Assert.ThrowsAsync<ApiRequestException>(() => policy.ExecuteAsync<object>(() =>
        {
            attempt++;
            throw NotRetryable();
        }));

        Assert.Equal(1, attempt);
        Assert.False(delayCalled);
    }
}
