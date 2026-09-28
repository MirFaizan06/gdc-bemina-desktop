namespace CollegeAdmin.Contracts.Api;

public sealed record AdmissionLinkDto
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string? Url { get; init; }
    public string? FilePath { get; init; }
    public string Status { get; init; } = "";
    public int SortOrder { get; init; }
}
public sealed record AdmissionLinksListResult { public IReadOnlyList<AdmissionLinkDto> AdmissionLinks { get; init; } = []; }

public sealed record AdmissionBannerDto
{
    public int Id { get; init; }
    public string PairKey { get; init; } = "";
    public string Label { get; init; } = "";
    public int SortOrder { get; init; }
    public bool Enabled { get; init; }
}
public sealed record AdmissionBannersListResult { public IReadOnlyList<AdmissionBannerDto> AdmissionBanners { get; init; } = []; }
