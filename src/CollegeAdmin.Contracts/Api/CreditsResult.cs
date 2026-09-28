namespace CollegeAdmin.Contracts.Api;

public sealed record CreditsResult
{
    public string Company { get; init; } = "";
    public string Developer { get; init; } = "";
    public string BusinessEmail { get; init; } = "";
}
