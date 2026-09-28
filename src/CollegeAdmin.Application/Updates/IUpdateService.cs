namespace CollegeAdmin.Application.Updates;

/// <summary>Result of comparing the running build against the server's published release manifest
/// (AppVersionController). A failed check (network error, malformed response) reports
/// IsUpdateAvailable=false rather than throwing — checking for updates must never be able to crash
/// or block the app (CLAUDE.md's "safe failure behavior" requirement for the updater).</summary>
public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string CurrentVersion,
    string LatestVersion,
    string? DownloadUrl,
    string? Sha256,
    string? ReleaseNotes);

/// <summary>Result of downloading and SHA-256-verifying an installer. Success is false — with
/// ErrorMessage explaining why — for every failure mode: no download published yet, network
/// failure, or (critically) a hash mismatch, in which case the downloaded file is deleted rather
/// than left around to potentially be run anyway.</summary>
public sealed record UpdateDownloadResult(bool Success, string? LocalFilePath, string? ErrorMessage);

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task<UpdateDownloadResult> DownloadAndVerifyAsync(UpdateCheckResult update, CancellationToken cancellationToken = default);

    /// <summary>Hands the verified installer to Windows (UAC will prompt as normal for an MSI) —
    /// does not touch this app's own process lifetime; the caller decides whether/when to exit.</summary>
    void LaunchInstaller(string installerPath);
}
