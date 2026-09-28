using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using CollegeAdmin.Application.Updates;

namespace CollegeAdmin.Infrastructure.Updates;

/// <summary>
/// Checks GitHub's public Releases API (`GET /repos/{owner}/{repo}/releases/latest`) directly — no
/// backend involvement at all, no server-hosted manifest to hand-edit per release. GitHub hosts the
/// MSI (and its SHA-256 sidecar file) as release assets for free, which is also a better fit than
/// college hosting bandwidth for a multi-megabyte installer (CLAUDE.md's "free to operate" goal).
/// The releases repo must be public: an unauthenticated request is the only option that doesn't
/// require embedding a token in the shipped client (CLAUDE.md's "no secrets embedded" rule) — a
/// private repo would need a PAT the app can't safely carry.
///
/// A release's assets must include both the `.msi` and a same-named `.sha256` sidecar (the exact
/// lowercase hex digest, nothing else) — the GitHub Actions release workflow in the desktop repo
/// produces both automatically.
///
/// Deliberately does NOT self-replace the running executable: DownloadAndVerifyAsync downloads to
/// a temp file and SHA-256-verifies it against the published hash, and LaunchInstaller hands the
/// verified file to Windows (a normal MSI double-click, UAC prompt included) rather than this
/// process silently overwriting its own files while running.
/// </summary>
public sealed class UpdateService(IHttpClientFactory httpClientFactory, string currentAppVersion, string githubOwner, string githubRepo) : IUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client = httpClientFactory.CreateClient("AuthApi");

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases/latest");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CollegeAdmin-Desktop", currentAppVersion));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // No release published yet (404 on a brand-new repo), rate-limited, GitHub down —
                // none of these should ever surface as an error just for checking.
                return new UpdateCheckResult(false, currentAppVersion, currentAppVersion, null, null, null);
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(JsonOptions, cancellationToken);
            if (release is null)
            {
                return new UpdateCheckResult(false, currentAppVersion, currentAppVersion, null, null, null);
            }

            var latestVersion = release.TagName.TrimStart('v', 'V');
            var msiAsset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));
            var sha256Asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase));

            var isNewer = TryParse(latestVersion, out var latest)
                && TryParse(currentAppVersion, out var current)
                && latest > current;

            string? sha256 = null;
            if (isNewer && sha256Asset is not null)
            {
                sha256 = (await _client.GetStringAsync(sha256Asset.BrowserDownloadUrl, cancellationToken)).Trim().Split(' ')[0];
            }

            return new UpdateCheckResult(isNewer, currentAppVersion, latestVersion, msiAsset?.BrowserDownloadUrl, sha256, release.Body);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new UpdateCheckResult(false, currentAppVersion, currentAppVersion, null, null, null);
        }
    }

    public async Task<UpdateDownloadResult> DownloadAndVerifyAsync(UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(update.DownloadUrl) || string.IsNullOrWhiteSpace(update.Sha256))
        {
            return new UpdateDownloadResult(false, null, "No download has been published for this release yet.");
        }

        if (!Uri.TryCreate(update.DownloadUrl, UriKind.Absolute, out var downloadUri) || !IsAllowedDownloadScheme(downloadUri))
        {
            return new UpdateDownloadResult(false, null,
                "The published update address is not secure (HTTPS required) and was rejected.");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"CollegeAdminSetup-{update.LatestVersion}.msi");
        try
        {
            using (var response = await _client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = File.Create(tempPath);
                await httpStream.CopyToAsync(fileStream, cancellationToken);
            }

            var actualHash = await ComputeSha256Async(tempPath, cancellationToken);
            if (!string.Equals(actualHash, update.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteQuietly(tempPath);
                return new UpdateDownloadResult(false, null,
                    "The downloaded installer failed integrity verification and was discarded. Please try again.");
            }

            return new UpdateDownloadResult(true, tempPath, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        {
            TryDeleteQuietly(tempPath);
            return new UpdateDownloadResult(false, null, "Could not download the update. Check your connection and try again.");
        }
    }

    public void LaunchInstaller(string installerPath) =>
        Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });

    // HTTPS is required for any real host. Loopback is allowed only so local testing keeps working.
    private static bool IsAllowedDownloadScheme(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps || uri.IsLoopback;

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }

    private static void TryDeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup only — a locked/undeletable temp file is not worth surfacing as
            // an additional error on top of the download/verification failure already reported.
        }
    }

    private static bool TryParse(string version, out Version parsed)
    {
        var ok = Version.TryParse(NormalizeToFourParts(version), out var result);
        parsed = result ?? new Version(0, 0);
        return ok;
    }

    // System.Version requires at least two components ("1.0"); a bare "1" tag would otherwise throw
    // instead of failing the update check gracefully.
    private static string NormalizeToFourParts(string version) =>
        version.Count(c => c == '.') == 0 ? $"{version}.0" : version;
}
