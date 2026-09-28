using System.Net;
using CollegeAdmin.Infrastructure.Connectivity;

namespace CollegeAdmin.Infrastructure.Tests;

/// <summary>Final Convergence Phase P1-16: proves the handler observes real request/response flow
/// rather than polling — a successful HTTP round trip (any status code) reports success, a transport
/// failure (HttpRequestException, the exception SocketsHttpHandler actually throws on an unreachable
/// server) reports failure.</summary>
public class ConnectivityTrackingHandlerTests
{
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    [Fact]
    public async Task SendAsync_OnSuccessfulRoundTrip_ReportsSuccess_RegardlessOfStatusCode()
    {
        var connectivity = new ConnectivityService();
        connectivity.ReportFailure(); // start offline so ReportSuccess is observable
        var handler = new ConnectivityTrackingHandler(connectivity) { InnerHandler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{}") };
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };

        await client.GetAsync("ping");

        Assert.True(connectivity.IsOnline);
    }

    [Fact]
    public async Task SendAsync_OnHttpRequestException_ReportsFailure_AndRethrows()
    {
        var connectivity = new ConnectivityService();
        var handler = new ConnectivityTrackingHandler(connectivity) { InnerHandler = new ThrowingHandler(new HttpRequestException("connection refused")) };
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("ping"));

        Assert.False(connectivity.IsOnline);
    }
}
