using System.Net;
using System.Text;

namespace CollegeAdmin.Infrastructure.Tests;

/// <summary>
/// Final Convergence Phase P1-5 (docs/claude/FINAL_COMPLETION_TRACKER.md §2): unlike
/// <see cref="FakeHttpMessageHandler"/> (one canned response for every request), this returns a
/// different body per request in the order they arrive — needed to simulate a real multi-page
/// list endpoint (page 1, then page 2, ...) without any real network access.
/// </summary>
internal sealed class SequencedHttpMessageHandler(params string[] jsonBodiesInOrder) : HttpMessageHandler
{
    private int _index;
    public List<string> RequestedUrls { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUrls.Add(request.RequestUri!.ToString());
        var body = jsonBodiesInOrder[Math.Min(_index, jsonBodiesInOrder.Length - 1)];
        _index++;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
