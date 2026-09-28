using System.Linq;
using System.Net;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Infrastructure.Tests;

/// <summary>
/// Verifies ApiClient against the exact envelope shapes captured from a live run of the real
/// server in Phase 1 Stage 1 (see docs/claude/18_DEVLOG.md, Entry 3) — deterministic and
/// network-free, but traceable back to real, observed server output rather than an assumption
/// about what the server returns.
/// </summary>
public class ApiClientTests
{
    private static ApiClient BuildClient(HttpStatusCode statusCode, string jsonBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, jsonBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };
        return new ApiClient(httpClient);
    }

    [Fact]
    public async Task PingAsync_ParsesSuccessEnvelope()
    {
        const string body = """
            {"success":true,"data":{"status":"ok","time":"2026-09-25T09:21:07Z","apiVersion":"v1"},"meta":{},"correlationId":"9ebbd825c21d17ff"}
            """;
        var client = BuildClient(HttpStatusCode.OK, body);

        var result = await client.PingAsync();

        Assert.Equal("ok", result.Status);
        Assert.Equal("v1", result.ApiVersion);
        Assert.Equal("2026-09-25T09:21:07Z", result.Time);
    }

    [Fact]
    public async Task PublishTimetableEntryAsync_ParsesTheToggledEntry()
    {
        // Captured from a real toggle against the local dev server (Final Convergence Phase P0-5,
        // docs/claude/18_DEVLOG.md) — status flips server-side; this call doesn't choose which way.
        const string body = """
            {"success":true,"data":{"entry":{"id":1,"academicYear":"2026-27","programmeId":1,"semester":1,"section":null,"programmePaperId":null,"subject":"Data Structures","facultyId":1,"room":"A101","dayOfWeek":"mon","startTime":"09:00:00","endTime":"10:00:00","status":"draft","departmentId":null,"committeeId":null,"createdAt":"2026-09-25 19:14:30","updatedAt":"2026-09-27 02:39:49"}},"meta":{},"correlationId":"50dcf5a9bd31ff88"}
            """;
        var client = BuildClient(HttpStatusCode.OK, body);

        var result = await client.PublishTimetableEntryAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("draft", result.Status);
        Assert.Equal("Data Structures", result.Subject);
    }

    [Fact]
    public async Task PingAsync_ThrowsApiRequestException_OnErrorEnvelope()
    {
        const string body = """
            {"success":false,"error":{"code":"NOT_FOUND","message":"No API route matches GET /api/v1/nope.","details":null,"fieldErrors":null,"retryable":false,"correlationId":"1c0e406e8a83a78a"}}
            """;
        var client = BuildClient(HttpStatusCode.NotFound, body);

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.PingAsync());

        Assert.Equal("NOT_FOUND", ex.Code);
        Assert.False(ex.Retryable);
        Assert.Equal("1c0e406e8a83a78a", ex.CorrelationId);
        Assert.Equal("No API route matches GET /api/v1/nope.", ex.Message);
    }

    /// <summary>
    /// Final Convergence Phase P1-5 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): a single
    /// perPage=200 request used to be trusted as "the whole list", silently truncating anything
    /// past 200 rows. This simulates a 250-row dataset split across 2 pages of 200 + 50 (matching
    /// StudentsController::index()'s real 200-row server-side cap) and confirms GetStudentsAsync
    /// now fetches both pages and returns all 250, not just the first 200.
    /// </summary>
    [Fact]
    public async Task GetStudentsAsync_FetchesAllPages_WhenTotalExceedsOnePage()
    {
        var page1Students = string.Join(',', Enumerable.Range(1, 200).Select(i => $"{{\"id\":{i},\"name\":\"Student {i}\"}}"));
        var page2Students = string.Join(',', Enumerable.Range(201, 50).Select(i => $"{{\"id\":{i},\"name\":\"Student {i}\"}}"));
        var page1 = $$"""{"success":true,"data":{"students":[{{page1Students}}]},"meta":{"page":1,"perPage":200,"total":250},"correlationId":"a"}""";
        var page2 = $$"""{"success":true,"data":{"students":[{{page2Students}}]},"meta":{"page":2,"perPage":200,"total":250},"correlationId":"b"}""";
        var handler = new SequencedHttpMessageHandler(page1, page2);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };
        var client = new ApiClient(httpClient);

        var result = await client.GetStudentsAsync();

        Assert.Equal(250, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(250, result[249].Id);
        Assert.Equal(2, handler.RequestedUrls.Count);
        Assert.Contains("page=1", handler.RequestedUrls[0]);
        Assert.Contains("page=2", handler.RequestedUrls[1]);
    }

    /// <summary>Final Convergence Phase P1-16: GetAsync-based reads retry a retryable failure
    /// automatically (see ApiEnvelopeHttp.GetPageAsync's default RetryPolicy) — the first request
    /// gets a retryable error envelope, the second (retried) request succeeds.</summary>
    /// <summary>Final Convergence Phase P2 item 2 (docs/claude/FINAL_COMPLETION_TRACKER.md §4): the
    /// `result` field's shape is captured from a real live run against the dev server (a failed
    /// university_rr_export job, `result: null`) — confirms the whole envelope still deserializes
    /// correctly with the new field present but null, matching every existing job before this item.</summary>
    [Fact]
    public async Task GetJobsAsync_ParsesAFailedJobEnvelope_WithNullResult()
    {
        const string body = """
            {"success":true,"data":{"jobs":[{"id":3,"type":"university_rr_export","status":"failed","payload":{"academicSession":"2026-27","semester":1,"programmeId":null},"progress":0,"createdBy":1,"createdAt":"2026-09-27 23:20:48","startedAt":"2026-09-27 23:20:48","finishedAt":"2026-09-27 23:20:48","errorCode":"EXPORT_ERROR","errorMessage":"No eligible students matched this scope.","retryCount":1,"idempotencyKey":null,"result":null}]},"meta":{},"correlationId":"x"}
            """;
        var client = BuildClient(HttpStatusCode.OK, body);

        var jobs = await client.GetJobsAsync();

        Assert.Single(jobs);
        Assert.Equal("university_rr_export", jobs[0].Type);
        Assert.Equal("failed", jobs[0].Status);
        Assert.Equal("EXPORT_ERROR", jobs[0].ErrorCode);
        Assert.Null(jobs[0].Result);
    }

    /// <summary>A success case's `result` object — the shape StudentImportsController::store() now
    /// records via BackgroundJob::markFinished($result).</summary>
    [Fact]
    public async Task GetJobsAsync_ParsesAFinishedJobEnvelope_WithAResultObject()
    {
        const string body = """
            {"success":true,"data":{"jobs":[{"id":4,"type":"student_import_stage","status":"finished","payload":{"filename":"roster.xlsx"},"progress":100,"createdBy":1,"createdAt":"2026-09-27 23:20:48","startedAt":"2026-09-27 23:20:48","finishedAt":"2026-09-27 23:20:49","errorCode":null,"errorMessage":null,"retryCount":0,"idempotencyKey":null,"result":{"batchId":42,"totalRows":50,"validRows":45,"warningRows":3,"errorRows":2}}]},"meta":{},"correlationId":"y"}
            """;
        var client = BuildClient(HttpStatusCode.OK, body);

        var jobs = await client.GetJobsAsync();

        Assert.NotNull(jobs[0].Result);
        var result = (System.Text.Json.JsonElement)jobs[0].Result!;
        Assert.Equal(42, result.GetProperty("batchId").GetInt32());
        Assert.Equal(45, result.GetProperty("validRows").GetInt32());
    }

    [Fact]
    public async Task PingAsync_RetriesOnRetryableFailure_ThenSucceeds()
    {
        const string errorBody = """{"success":false,"error":{"code":"SERVER_ERROR","message":"Temporary failure.","details":null,"fieldErrors":null,"retryable":true,"correlationId":"x"}}""";
        const string successBody = """{"success":true,"data":{"status":"ok","time":"2026-09-27T00:00:00Z","apiVersion":"v1"},"meta":{},"correlationId":"y"}""";
        var handler = new SequencedHttpMessageHandler(errorBody, successBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };
        var client = new ApiClient(httpClient);

        var result = await client.PingAsync();

        Assert.Equal("ok", result.Status);
        Assert.Equal(2, handler.RequestedUrls.Count);
    }

    /// <summary>Final Convergence Phase P1-16: the tracker's own explicit test requirement — a
    /// destructive verb (create, in this case, but every write in this API funnels through the same
    /// PostAsync primitive DeleteGenericAsync/UpdateGenericAsync also use) must never be
    /// auto-retried, even when the server marks the failure retryable. Exactly one request should be
    /// made before the exception propagates.</summary>
    [Fact]
    public async Task CreateGenericAsync_NeverRetries_EvenOnARetryableFailure()
    {
        const string errorBody = """{"success":false,"error":{"code":"SERVER_ERROR","message":"Temporary failure.","details":null,"fieldErrors":null,"retryable":true,"correlationId":"x"}}""";
        var handler = new SequencedHttpMessageHandler(errorBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };
        var client = new ApiClient(httpClient);

        var ex = await Assert.ThrowsAsync<ApiRequestException>(() => client.CreateGenericAsync("notices", new Dictionary<string, string?>()));

        Assert.True(ex.Retryable);
        Assert.Single(handler.RequestedUrls);
    }

    [Fact]
    public async Task GetStudentsAsync_StopsAtOnePage_WhenTotalFitsWithinIt()
    {
        const string body = """{"success":true,"data":{"students":[{"id":1,"name":"Only Student"}]},"meta":{"page":1,"perPage":200,"total":1},"correlationId":"a"}""";
        var handler = new SequencedHttpMessageHandler(body);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };
        var client = new ApiClient(httpClient);

        var result = await client.GetStudentsAsync();

        Assert.Single(result);
        Assert.Single(handler.RequestedUrls);
    }
}
