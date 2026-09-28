namespace CollegeAdmin.Contracts.Api;

/// <summary>Mirrors AppVersionController::show()'s response — the desktop updater's source of
/// truth for whether a newer build exists.</summary>
public sealed record AppVersionInfo
{
    public string LatestVersion { get; init; } = "0.0.0";
    public string MinimumRequiredVersion { get; init; } = "0.0.0";
    public string? DownloadUrl { get; init; }
    public string? Sha256 { get; init; }
    public string? ReleaseNotes { get; init; }
    public string? PublishedAt { get; init; }
}
