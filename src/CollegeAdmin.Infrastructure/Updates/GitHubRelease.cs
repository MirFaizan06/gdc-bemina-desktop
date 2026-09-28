using System.Text.Json.Serialization;

namespace CollegeAdmin.Infrastructure.Updates;

/// <summary>Shape of GitHub's public `GET /repos/{owner}/{repo}/releases/latest` response — only
/// the fields this app actually reads. GitHub's REST API is stable/versioned; no auth needed for a
/// public repo's releases.</summary>
public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset> Assets { get; set; } = [];
}

public sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";
}
