using System.Net;
using System.Text;

namespace CollegeAdmin.Infrastructure.Tests;

/// <summary>Returns one canned response for every request — no real network access.</summary>
internal sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string jsonBody) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
