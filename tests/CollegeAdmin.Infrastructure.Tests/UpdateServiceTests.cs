using System.Net;
using System.Security.Cryptography;
using System.Text;
using CollegeAdmin.Application.Updates;
using CollegeAdmin.Infrastructure.Updates;

namespace CollegeAdmin.Infrastructure.Tests;

/// <summary>Routes by request URI rather than returning one canned response for every call (unlike
/// FakeHttpMessageHandler) — UpdateService's CheckForUpdateAsync makes up to two calls (the GitHub
/// releases API, then the .sha256 sidecar asset) and DownloadAndVerifyAsync makes a third (the MSI
/// asset), which need different bodies.</summary>
internal sealed class RoutingFakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(respond(request));
}

internal sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}

public class UpdateServiceTests
{
    private static UpdateService BuildService(Func<HttpRequestMessage, HttpResponseMessage> respond, string currentVersion = "1.0.0")
    {
        var handler = new RoutingFakeHttpMessageHandler(respond);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };
        return new UpdateService(new FakeHttpClientFactory(httpClient), currentVersion, "owner", "repo");
    }

    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage TextResponse(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "text/plain"),
    };

    [Fact]
    public async Task CheckForUpdateAsync_NewerVersionPublished_ReportsUpdateAvailable_AndFetchesTheSha256Sidecar()
    {
        const string release = """
            {"tag_name":"v1.2.0","body":"Notes","assets":[
                {"name":"CollegeAdminSetup-1.2.0.msi","browser_download_url":"https://example.test/setup.msi"},
                {"name":"CollegeAdminSetup-1.2.0.msi.sha256","browser_download_url":"https://example.test/setup.msi.sha256"}
            ]}
            """;
        var service = BuildService(req => req.RequestUri!.AbsoluteUri.Contains("releases/latest")
            ? JsonResponse(release)
            : TextResponse("abc123hash\n"), currentVersion: "1.0.0");

        var result = await service.CheckForUpdateAsync();

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Equal("1.2.0", result.LatestVersion);
        Assert.Equal("https://example.test/setup.msi", result.DownloadUrl);
        Assert.Equal("abc123hash", result.Sha256);
        Assert.Equal("Notes", result.ReleaseNotes);
    }

    [Fact]
    public async Task CheckForUpdateAsync_SameVersionAsTag_ReportsNoUpdate_AndDoesNotFetchTheSidecar()
    {
        const string release = """
            {"tag_name":"v1.0.0","body":null,"assets":[
                {"name":"CollegeAdminSetup-1.0.0.msi","browser_download_url":"https://example.test/setup.msi"}
            ]}
            """;
        var sidecarFetched = false;
        var service = BuildService(req =>
        {
            if (!req.RequestUri!.AbsoluteUri.Contains("releases/latest")) sidecarFetched = true;
            return JsonResponse(release);
        }, currentVersion: "1.0.0");

        var result = await service.CheckForUpdateAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.False(sidecarFetched);
    }

    [Fact]
    public async Task CheckForUpdateAsync_OnNetworkFailure_ReportsNoUpdate_DoesNotThrow()
    {
        var handler = new RoutingFakeHttpMessageHandler(_ => throw new HttpRequestException("offline"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/v1/") };
        var service = new UpdateService(new FakeHttpClientFactory(httpClient), "1.0.0", "owner", "repo");

        var result = await service.CheckForUpdateAsync();

        Assert.False(result.IsUpdateAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_NoReleasePublishedYet_ReportsNoUpdate()
    {
        var service = BuildService(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await service.CheckForUpdateAsync();

        Assert.False(result.IsUpdateAvailable);
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_NoDownloadPublished_FailsWithClearMessage_MakesNoHttpCall()
    {
        var called = false;
        var service = BuildService(_ => { called = true; return JsonResponse("{}"); });
        var update = new UpdateCheckResult(true, "1.0.0", "1.2.0", null, null, null);

        var result = await service.DownloadAndVerifyAsync(update);

        Assert.False(result.Success);
        Assert.Equal("No download has been published for this release yet.", result.ErrorMessage);
        Assert.False(called);
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_HashMatches_SucceedsAndLeavesTheVerifiedFileOnDisk()
    {
        var payload = "fake-installer-bytes"u8.ToArray();
        var correctHash = Convert.ToHexStringLower(SHA256.HashData(payload));
        var service = BuildService(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
        var update = new UpdateCheckResult(true, "1.0.0", "1.2.0", "https://example.test/setup.msi", correctHash, null);

        var result = await service.DownloadAndVerifyAsync(update);
        try
        {
            Assert.True(result.Success);
            Assert.NotNull(result.LocalFilePath);
            Assert.True(File.Exists(result.LocalFilePath));
            Assert.Equal(payload, await File.ReadAllBytesAsync(result.LocalFilePath!));
        }
        finally
        {
            if (result.LocalFilePath is not null && File.Exists(result.LocalFilePath))
            {
                File.Delete(result.LocalFilePath);
            }
        }
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_InsecureProductionUrl_RejectsWithoutDownloadingAndMakesNoHttpCall()
    {
        var called = false;
        var service = BuildService(_ => { called = true; return JsonResponse("{}"); });
        var update = new UpdateCheckResult(true, "1.0.0", "1.2.0", "http://example.test/setup.msi", "abc", null);

        var result = await service.DownloadAndVerifyAsync(update);

        Assert.False(result.Success);
        Assert.Null(result.LocalFilePath);
        Assert.Contains("not secure", result.ErrorMessage);
        Assert.False(called);
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_LoopbackHttpUrl_IsAllowedForLocalDev()
    {
        var payload = "fake-installer-bytes"u8.ToArray();
        var correctHash = Convert.ToHexStringLower(SHA256.HashData(payload));
        var service = BuildService(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
        var update = new UpdateCheckResult(true, "1.0.0", "1.2.0", "http://localhost:8000/setup.msi", correctHash, null);

        var result = await service.DownloadAndVerifyAsync(update);
        try
        {
            Assert.True(result.Success);
        }
        finally
        {
            if (result.LocalFilePath is not null && File.Exists(result.LocalFilePath))
            {
                File.Delete(result.LocalFilePath);
            }
        }
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_HashMismatch_FailsAndDeletesTheFile()
    {
        var payload = "fake-installer-bytes"u8.ToArray();
        var service = BuildService(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
        var update = new UpdateCheckResult(true, "1.0.0", "1.2.0", "https://example.test/setup.msi", "0000000000000000000000000000000000000000000000000000000000000000", null);

        var result = await service.DownloadAndVerifyAsync(update);

        Assert.False(result.Success);
        Assert.Null(result.LocalFilePath);
        Assert.Contains("integrity verification", result.ErrorMessage);
        Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), "CollegeAdminSetup-1.2.0.msi")));
    }
}
